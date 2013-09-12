// Mono.WebServer.BaseApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO.Foo;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Collections.Generic;
using Mono.WebServer.Log;

namespace Mono.WebServer
{
	public class BaseApplicationHost : MarshalByRefObject, IApplicationHost
	{
		static readonly ReaderWriterLockSlim handlersCacheLock = new ReaderWriterLockSlim ();
		
		string path;
		string vpath;
		readonly EndOfRequestHandler endOfRequest;
		Dictionary <string, bool> handlersCache;		

		public BaseApplicationHost ()
		{
			endOfRequest = EndOfRequest;
			AppDomain.CurrentDomain.DomainUnload += OnUnload;
		}

		public void Unload ()
		{
			HttpRuntime.UnloadAppDomain ();
		}

		public void OnUnload (object o, EventArgs args)
		{
			if (Server != null)
				Server.DestroyHost (this);
		}

		public override object InitializeLifetimeService ()
		{
			return null; // who wants to live forever?
		}

		public ApplicationServer Server { get; set; }

		public string Path {
			get {
				if (path == null)
					path = AppDomain.CurrentDomain.GetData (".appPath").ToString ();

				return path;
			}
		}

		public string VPath {
			get {
				if (vpath == null)
					vpath = AppDomain.CurrentDomain.GetData (".appVPath").ToString ();

				return vpath;
			}
		}

		public AppDomain Domain {
			get { return AppDomain.CurrentDomain; }
		}

		public IRequestBroker RequestBroker { get; set; }

		protected void ProcessRequest (MonoWorkerRequest mwr)
		{
			if (mwr == null)
				throw new ArgumentNullException ("mwr");
			
			if (!mwr.ReadRequestData ()) {
				EndOfRequest (mwr);
				return;
			}
			
			mwr.EndOfRequestEvent += endOfRequest;
			try {
				mwr.ProcessRequest ();
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			} catch (Exception ex) { // should "never" happen
				// we don't know what the request state is,
				// better write the exception to the console
				// than forget it.
				Logger.Write(LogLevel.Error, "Unhandled exception: {0}", ex);
				EndOfRequest (mwr);
			}
		}

		public void EndOfRequest (MonoWorkerRequest mwr)
		{
			try {
				mwr.CloseConnection ();
			} catch {
			} finally {
				var brb = RequestBroker as BaseRequestBroker;
				if (brb != null)
					brb.UnregisterRequest (mwr.RequestId);
			}
		}

		public virtual bool IsHttpHandler (string verb, string uri)
		{
			string cacheKey = verb + "_" + uri;

			bool locked = false;
			try {
				handlersCacheLock.EnterReadLock ();

				locked = true;
				if (handlersCache != null) {
					bool found;
					if (handlersCache.TryGetValue (cacheKey, out found))
						return found;
				} else {
					handlersCache = new Dictionary <string, bool> ();
				}
			} finally {
				if (locked)
					handlersCacheLock.ExitReadLock ();
			}
			
			
			bool handlerFound = LocateHandler (verb, uri);
			locked = false;
			try {
				handlersCacheLock.EnterWriteLock ();

				locked = true;
				if (handlersCache.ContainsKey (cacheKey))
					handlersCache [cacheKey] = handlerFound;
				else
					handlersCache.Add (cacheKey, handlerFound);
			} finally {
				if (locked)
					handlersCacheLock.ExitWriteLock ();
			}
			
			return handlerFound;
		}

		bool LocateHandler (string verb, string uri)
		{
			var config = WebConfigurationManager.GetSection ("system.web/httpHandlers") as HttpHandlersSection;
			HttpHandlerActionCollection handlers = config != null ? config.Handlers : null;
			int count = handlers != null ? handlers.Count : 0;
			
			if (count == 0)
				return false;

			for (int i = 0; i < count; i++) {
				HttpHandlerAction handler = handlers [i];
				string[] verbs = SplitVerbs (handler.Verb);

				if (verbs == null) {
					if (PathMatches (handler, uri))
						return true;
					continue;
				}

				for (int j = 0; j < verbs.Length; j++) {
					if (verbs [j] != verb)
						continue;
					if (PathMatches (handler, uri))
						return true;
				}
			}

			return false;
		}

		bool PathMatches (HttpHandlerAction handler, string uri)
		{
			bool result = false;
			string[] handlerPaths = handler.Path.Split (',');
			int slash = uri.LastIndexOf ('/');
			string origUri = uri;
			if (slash != -1)
				uri = uri.Substring (slash);

			SearchPattern sp = null;
			foreach (string handlerPath in handlerPaths) {
				if (handlerPath == "*")
					continue; // ignore
				
				string matchExact = null;
				string endsWith = null;

				if (handlerPath.Length > 0) {
					if (handlerPath [0] == '*' && (handlerPath.IndexOf ('*', 1) == -1))
						endsWith = handlerPath.Substring (1);

					if (handlerPath.IndexOf ('*') == -1)
					if (handlerPath [0] != '/') {
						string vpath = HttpRuntime.AppDomainAppVirtualPath;

						if (vpath == "/")
							vpath = String.Empty;

						matchExact = String.Concat (vpath, "/", handlerPath);
					}
				}

				if (matchExact != null) {
					result = matchExact.Length == origUri.Length && origUri.EndsWith (matchExact, StringComparison.OrdinalIgnoreCase);
					if (result)
						break;
					continue;
				}
				if (endsWith != null) {
					result = uri.EndsWith (endsWith, StringComparison.OrdinalIgnoreCase);
					if (result)
						break;
					continue;
				}

				string pattern;
				if (handlerPath.Length > 0 && handlerPath [0] == '/')
					pattern = handlerPath.Substring (1);
				else
					pattern = handlerPath;

				if (sp == null)
					sp = new SearchPattern (pattern, true);
				else
					sp.SetPattern (pattern, true);
				
				if (sp.IsMatch (origUri)) {
					result = true;
					break;
				}
			}
			
			return result;
		}

		static string[] SplitVerbs (string verb)
		{
			return verb == "*" ? null : verb.Split (',');
		}
	}
}

