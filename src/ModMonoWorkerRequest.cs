//
// ModMonoWorkerRequest.cs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
// (C) Copyright 2004,2005 Novell, Inc. (http://www.novell.com)
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
using System.Web;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Mono.WebServer
{
	public class RequestReader
	{
		ModMonoRequest request;

		public ModMonoRequest Request {
			get { return request; }
		}
		
		public RequestReader (NetworkStream ns)
		{
			this.request = new ModMonoRequest (ns);
		}

		public string GetUriPath ()
		{
			string path = request.GetUri ();

			int dot = path.LastIndexOf ('.');
			int slash = (dot != -1) ? path.IndexOf ('/', dot) : 0;
			if (dot > 0 && slash > 0)
				path = path.Substring (0, slash);

			return path;
		}

		public string GetPhysicalPath ()
		{
			return request.GetPhysicalPath ();
		}

		public void Decline ()
		{
			request.Decline ();
		}

		public void NotFound ()
		{
			request.NotFound ();
		}

		public bool ShuttingDown {
			get { return request.ShuttingDown; }
		}
	}

	public class ModMonoWorkerRequest : MonoWorkerRequest
	{
		bool closed;
		string verb;
		string queryString;
		string protocol;
		string path;
		string pathInfo;
		string localAddress;
		int serverPort;
		string remoteAddress;
		int remotePort;
		string remoteName;
		ModMonoRequestBroker requestBroker;
		string[] headers;
		int [] headersHash;
		string[] headerValues;
		int requestId;
		bool gotSecure;
		bool isSecure;

		string [][] unknownHeaders;
		static string [] indexFiles = { "index.aspx",
						"Default.aspx",
						"default.aspx",
						"index.html",
						"index.htm" };

		static ModMonoWorkerRequest ()
		{
#if NET_2_0
			string indexes = ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"];
#else
			string indexes = ConfigurationSettings.AppSettings ["MonoServerDefaultIndexFiles"];
#endif
			SetDefaultIndexFiles (indexes);
		}

		static void SetDefaultIndexFiles (string list)
		{
			if (list == null)
				return;

			ArrayList files = new ArrayList ();
			string [] fs = list.Split (',');
			foreach (string f in fs) {
				string trimmed = f.Trim ();
				if (trimmed == "") 
					continue;

				files.Add (trimmed);
			}

			indexFiles = (string []) files.ToArray (typeof (string));
		}

		public ModMonoWorkerRequest (int requestId, ModMonoRequestBroker requestBroker,
					IApplicationHost appHost, string verb, string path,
					string queryString, string protocol, string localAddress,
					int serverPort, string remoteAddress, int remotePort,
					string remoteName, string[] headers, string[] headerValues)
			: base (appHost)
		{
			this.requestId = requestId;
			this.requestBroker = requestBroker;
			this.verb = verb;
			//this.protocol = protocol;
			// Don't let System.Web know if it's 1.1. This way apache handles the chunked
			// encoding for us, without sys.web interfering.
			this.protocol = "HTTP/1.0";
			this.queryString = queryString;
			this.path = path;
			this.localAddress = localAddress;
			this.serverPort = serverPort;
			this.remoteAddress = remoteAddress;
			this.remotePort = remotePort;
			this.remoteName = remoteName;
			this.headers = headers;
			this.headerValues = headerValues;
		}

		public override int RequestId {
			get { return requestId; }
		}

		public override string GetPathInfo ()
		{
			return pathInfo;
		}

		public override string GetRawUrl ()
		{
			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			if (queryString != null && queryString.Length > 0)
				return result + "?" + queryString;

			return result;
		}

		bool TryDirectory ()
		{
			string localPath = GetFilePathTranslated ();
			
			if (!Directory.Exists (localPath))
				return false;

			string oldPath = path;
			if (!path.EndsWith ("/"))
				path += "/";

			bool catOne = false;
			foreach (string indexFile in indexFiles) {
				string testfile = Path.Combine (localPath, indexFile);
				if (File.Exists (testfile)) {
					path += indexFile;
					catOne = true;
					break;
				}
			}

			if (!catOne)
				path = oldPath;

			return true;
		}

		protected override bool GetRequestData ()
		{
			if (TryDirectory ()) {
				pathInfo = "";
				return true;
			}

			string old_path = path;
			Paths.GetPathsFromUri (old_path, out path, out pathInfo);
			if (path [path.Length - 1] == '/')
				path = path + indexFiles [0];
			return true;
		}
		
		public override bool HeadersSent ()
		{
			//FIXME!!!!: how do we know this?
			return false;
		}
		
		public override void FlushResponse (bool finalFlush)
		{
			// FLUSH is a no-op in mod_mono. Apache takes care of it.
			// requestBroker.Flush (requestId);
			if (finalFlush)
				CloseConnection ();
		}

		public override bool IsSecure ()
		{
			if (!gotSecure) {
				string val = requestBroker.GetServerVariable (requestId, "SERVER_PORT_SECURE");
				isSecure =  (val != null && val != "");
				gotSecure = true;
			}

			return isSecure;
		}

		public override void CloseConnection ()
		{
			if (!closed) {
				requestBroker.Close (requestId);
				closed = true;
			}
		}

		public override string GetHttpVerbName ()
		{
			return verb;
		}

		public override string GetHttpVersion ()
		{
			return protocol;
		}

		public override string GetLocalAddress ()
		{
			return localAddress;
		}

		public override int GetLocalPort ()
		{
			return serverPort;
		}

		public override string GetQueryString ()
		{
			return queryString;
		}

		public override string GetRemoteAddress ()
		{
			return remoteAddress;
		}

		public override int GetRemotePort ()
		{
			return remotePort;
		}

		Hashtable server_vars = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant,
							CaseInsensitiveComparer.DefaultInvariant);
		public override string GetServerVariable (string name)
		{
			object o = server_vars [name];
			if (o is bool)
				return null;

			if (o != null)
				return (string) o;

			string result = requestBroker.GetServerVariable (requestId, name);
			if (result != null && result.Length > 0) {
				server_vars [name] = result;
				return result;
			}

			switch (name) {
			case "HTTPS":
				result = (IsSecure ()) ? "on" : "off";
				server_vars ["HTTPS"] = result;
				break;
			default:
				result = base.GetServerVariable (name);
				if (result == null || result.Length == 0)
					server_vars [name] = false;
				else
					server_vars [name] = result;
				break;
			}

			return result;
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			if (data.Length > length * 2) {
				byte [] tmpbuffer = new byte [length];
				Buffer.BlockCopy (data, 0, tmpbuffer, 0, length);
				requestBroker.Write (requestId, tmpbuffer, 0, length);
			} else {
				requestBroker.Write (requestId, data, 0, length);
			}
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			requestBroker.SetStatusCodeLine (requestId, statusCode, String.Format("{0} {1}", statusCode, statusDescription));
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			requestBroker.SetResponseHeader (requestId, name, value);
		}

		public override bool IsClientConnected ()
		{
			return (requestBroker != null && requestBroker.IsConnected (requestId));
		}

		public override string GetUriPath ()
		{
			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			return result;
		}

		public override string GetFilePath ()
		{
			return path;
		}
		
		public override string GetRemoteName ()
		{
			return remoteName;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			return GetRequestHeader (name);
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			if (unknownHeaders == null) {
				int count = headers.Length;
				ArrayList pairs = new ArrayList ();
				for (int i = 0; i < count; i++) {
					if (HttpWorkerRequest.GetKnownRequestHeaderIndex (headers [i]) != -1)
						continue;

					pairs.Add (new string [] { headers [i], headerValues [i]});
				}
				
				if (pairs.Count != 0) {
					unknownHeaders = new string [pairs.Count][];
					for (int i = 0; i < pairs.Count; i++)
						unknownHeaders [i] = (string []) pairs [i];
				}
			}

			return unknownHeaders;
		}

		public override string GetKnownRequestHeader (int index)
		{
			return GetRequestHeader (GetKnownRequestHeaderName (index));
		}
		
		string GetRequestHeader (string name)
		{
			IHashCodeProvider hp = CaseInsensitiveHashCodeProvider.DefaultInvariant;
			if (headersHash == null) {
				headersHash = new int [headers.Length];
				for (int i = 0; i < headers.Length; i++) {
					headersHash [i] = hp.GetHashCode (headers [i]);
				}
			}

			int k = Array.IndexOf (headersHash, hp.GetHashCode (name));
			return (k == -1) ? null : headerValues [k];
		}

		public override void SendCalculatedContentLength (int contentLength) 
		{
			// Do nothing
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			if (buffer == null || size <= 0)
				return 0;

			byte [] readBuffer;
			int nr = requestBroker.Read (requestId, size, out readBuffer);
			Buffer.BlockCopy (readBuffer, 0, buffer, 0, nr);
			return nr;
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			if (offset == 0) {
				FileInfo info = new FileInfo (filename);
				if (info.Length == length) {
					requestBroker.SendFile (requestId, filename);
					return;
				}
			}

			FileStream file = null;
			try {
				file = File.OpenRead (filename);
				base.SendResponseFromFile (file.Handle, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			Stream file = null;
			try {
				file = new FileStream (handle, FileAccess.Read);
				SendFromStream (file, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}
	}
}

