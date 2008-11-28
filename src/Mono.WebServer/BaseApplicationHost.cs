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
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

#if NET_2_0
using System.Collections.Generic;
#endif

namespace Mono.WebServer
{
	public class BaseApplicationHost : MarshalByRefObject, IApplicationHost
	{
#if NET_2_0
		static readonly object matchedPathsCacheLock = new object ();
		static readonly object cachedMatchesLock = new object ();
#endif
		static readonly object handlersCacheLock = new object ();
		
		string path;
		string vpath;
		IRequestBroker requestBroker;
		EndOfRequestHandler endOfRequest;
		ApplicationServer appserver;
#if NET_2_0
		Dictionary <string, bool> handlersCache;
		Dictionary <HttpHandlerAction, Dictionary <string, bool>> matchedPathsCache;
#else
		Hashtable handlersCache;
#endif
		
		/// <summary>
		///   Creates the <see cref="EndOfRequest"/> event handler and registers
		///   a handler (<see cref="OnUnload"/>) with the <see cref="AppDomain.DomainUnload"/> event.
		/// </summary>
		public BaseApplicationHost ()
		{
			endOfRequest = new EndOfRequestHandler (EndOfRequest);
			AppDomain.CurrentDomain.DomainUnload += new EventHandler (OnUnload);
		}

		/// <summary>
		///   Unloads the current application domain by calling <see cref="HttpRuntime.UnloadAppDomain"/>
		/// </summary>
		public void Unload ()
		{
			HttpRuntime.UnloadAppDomain ();
		}

		/// <summary>
		///   Event handler for the <see cref="System.AppDomain.DomainUnload"/>
		///   event. Calls <see cref="ApplicationServer.DestroyHost"/>
		///   to shut the host down.
		/// </summary>
		public void OnUnload (object o, EventArgs args)
		{
			if (appserver != null)
				appserver.DestroyHost (this);
		}

		public override object InitializeLifetimeService ()
		{
			return null; // who wants to live forever?
		}

		/// <summary>
		///   Reference to the associated <see cref="ApplicationServer"/>
		/// </summary>
		public ApplicationServer Server {
			get { return appserver; }
			set { appserver = value; }
		}

		/// <summary>
		///   Physical path to the application root directory.
		/// </summary>
		public string Path {
			get {
				if (path == null)
					path = AppDomain.CurrentDomain.GetData (".appPath").ToString ();

				return path;
			}
		}

		/// <summary>
		///   Virtual path to the application root.
		/// </summary>
		public string VPath {
			get {
				if (vpath == null)
					vpath =  AppDomain.CurrentDomain.GetData (".appVPath").ToString ();

				return vpath;
			}
		}

		/// <summary>
		///   Returns the current application domain.
		/// </summary>
		public AppDomain Domain {
			get { return AppDomain.CurrentDomain; }
		}

		/// <summary>
		///   Reference to the associated request broker
		/// </summary>
		public IRequestBroker RequestBroker
		{
			get { return requestBroker; }
			set { requestBroker = value; }
		}

		/// <summary>
		///   Process a request.
		/// </summary>
		/// <param name="mwr">A worker object to actually process the request</param>
		/// <remarks>
		///   If the mwr parameter is null or no request data can be read, the request will be ended
		///   immediately. Otherwise, registers an event handler for the worker's <see
		///   cref="MonoWorkerRequest.EndOfRequest"/> event and calls the worker's <see
		///   cref="MonoWorkerRequest.ProcessRequest"/> method to actually process the request. If an unhandled exception
		///   occurs during that phase, it is printed to the console and <see cref="EndOfRequest"/> is called
		///   immediately.
		/// </remarks>
		protected void ProcessRequest (MonoWorkerRequest mwr)
		{
			if (mwr == null) {
				EndOfRequest (mwr);
				return;
			}
			
			if (!mwr.ReadRequestData ()) {
				EndOfRequest (mwr);
				return;
			}
			
			mwr.EndOfRequestEvent += endOfRequest;
			try {
				mwr.ProcessRequest ();
			} catch (Exception ex) { // should "never" happen
				// we don't know what the request state is,
				// better write the exception to the console
				// than forget it.
				Console.WriteLine ("Unhandled exception: {0}", ex);
				EndOfRequest (mwr);
			}
		}

		public void EndOfRequest (MonoWorkerRequest mwr)
		{
			try {
				mwr.CloseConnection ();
			} catch {
			} finally {
				BaseRequestBroker brb = requestBroker as BaseRequestBroker;
				if (brb != null)
					brb.UnregisterRequest (mwr.RequestId);
			}
		}

		/// <summary>
		///    Checks if the passed URI maps to a HTTP handler
		/// </summary>
		public virtual bool IsHttpHandler (string verb, string uri)
		{
			string cacheKey = verb + "_" + uri;
			lock (handlersCacheLock) {
				if (handlersCache != null) {
					bool found;
#if NET_2_0
					if (handlersCache.TryGetValue (cacheKey, out found))
						return found;
#else
					if (handlersCache.ContainsKey (cacheKey))
						return (bool) handlersCache [cacheKey];
#endif
				} else {
			
#if NET_2_0
					handlersCache = new Dictionary <string, bool> ();
#else
					handlersCache = new Hashtable ();
#endif
				}
			}
			
			bool handlerFound = LocateHandler (verb, uri);

			lock (handlersCacheLock) {
				handlersCache.Add (cacheKey, handlerFound);
			}
			
			return handlerFound;
		}

		bool LocateHandler (string verb, string uri)
		{
#if NET_2_0
			HttpHandlersSection config = WebConfigurationManager.GetSection ("system.web/httpHandlers") as HttpHandlersSection;
			HttpHandlerActionCollection handlers = config != null ? config.Handlers : null;
			int count = handlers != null ? handlers.Count : 0;
			
			if (count == 0)
				return false;

			HttpHandlerAction handler;
			string[] verbs;
			for (int i = 0; i < count; i++) {
				handler = handlers [i];
				verbs = SplitVerbs (handler.Verb);

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
#endif
			return false;
		}

#if NET_2_0
		bool PathMatches (HttpHandlerAction handler, string uri)
		{
			Dictionary <string, bool> cachedMatches;
			lock (matchedPathsCacheLock) {
				if (matchedPathsCache == null) {
					cachedMatches = new Dictionary <string, bool> ();
					matchedPathsCache = new Dictionary <HttpHandlerAction, Dictionary <string, bool>> ();
					matchedPathsCache.Add (handler, cachedMatches);
				} else {
					if (!matchedPathsCache.TryGetValue (handler, out cachedMatches)) {
						cachedMatches = new Dictionary <string, bool> ();
						matchedPathsCache.Add (handler, cachedMatches);
					}
				}
			}

			bool result = false;
			lock (cachedMatchesLock) {
				if (cachedMatches.TryGetValue (uri, out result))
					return result;
			}			

			string[] handlerPaths = handler.Path.Split (',');
			int slash = uri.LastIndexOf ('/');
			string origUri = uri;
			if (slash != -1)
				uri = uri.Substring (slash);

			foreach (string handlerPath in handlerPaths) {
				if (handlerPath == "*") {
					result = true;
					break;
				}

				string matchExact = null;
				string endsWith = null;
				Regex regEx = null;

				if (handlerPath.Length > 0) {
					if (handlerPath [0] == '*' && (handlerPath.IndexOf ('*', 1) == -1))
						endsWith = handlerPath.Substring (1);

					if (handlerPath.IndexOf ('*') == -1)
						if (handlerPath [0] != '/')
						{
							HttpContext ctx = HttpContext.Current;
							HttpRequest req = ctx != null ? ctx.Request : null;
							string vpath = HttpRuntime.AppDomainAppVirtualPath;

							if (vpath == "/")
								vpath = String.Empty;

							matchExact = String.Concat (vpath, "/", handlerPath);
						}
				}

				if (matchExact != null) {
					result = matchExact.Length == origUri.Length && origUri.EndsWith (matchExact, StringComparison.OrdinalIgnoreCase);
					if (result == true)
						break;
					else
						continue;
				} else if (endsWith != null) {
					result = uri.EndsWith (endsWith, StringComparison.OrdinalIgnoreCase);
					if (result == true)
						break;
					else
						continue;
				}

				if (handlerPath != "*") {
					string expr = handlerPath.Replace (".", "\\.").Replace ("?", "\\?").Replace ("*", ".*");
					if (expr.Length > 0 && expr [0] == '/')
						expr = expr.Substring (1);

					expr += "\\z";
					regEx = new Regex (expr, RegexOptions.IgnoreCase);

					if (regEx.IsMatch (origUri)) {
						result = true;
						break;
					}
				}
			}

			lock (cachedMatchesLock) {
				if (!cachedMatches.ContainsKey (origUri))
					cachedMatches.Add (origUri, result);
			}
			
			return result;
		}
#else
		bool PathMatches (object handler, string uri)
		{
			return false;
		}
#endif
		
		string[] SplitVerbs (string verb)
		{
			if (verb == "*")
				return null;

			return verb.Split (',');
		}
	}
}

