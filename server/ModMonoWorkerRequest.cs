/* ====================================================================
 * The XSP Software License, Version 1.1
 *
 * Authors:
 *	Daniel Lopez Ridruejo
 * 	Gonzalo Paniagua Javier
 *
 * Copyright (c) 2002 Daniel Lopez Ridruejo.
 *           (c) 2002,2003 Ximian, Inc.
 *           All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 *
 * 3. The end-user documentation included with the redistribution,
 *    if any, must include the following acknowledgment:
 *       "This product includes software developed by 
 *        Daniel Lopez Ridruejo (daniel@rawbyte.com) and
 *        Ximian Inc. (http://www.ximian.com)"
 *    Alternately, this acknowledgment may appear in the software itself,
 *    if and wherever such third-party acknowledgments normally appear.
 *
 * 4. The name "mod_mono" must not be used to endorse or promote products 
 *    derived from this software without prior written permission. For written
 *    permission, please contact daniel@rawbyte.com.
 *
 * 5. Products derived from this software may not be called "mod_mono",
 *    nor may "mod_mono" appear in their name, without prior written
 *    permission of Daniel Lopez Ridruejo and Ximian Inc.
 *
 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED.  IN NO EVENT SHALL DANIEL LOPEZ RIDRUEJO OR
 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 * ====================================================================
 *
 */
using System;
using System.Web;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Mono.ASPNET
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

		public void Decline ()
		{
			request.Decline ();
		}

		public void NotFound ()
		{
			request.NotFound ();
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

		string [][] unknownHeaders;
		static string [] indexFiles = { "index.aspx",
						"Default.aspx",
						"default.aspx",
						"index.html",
						"index.htm" };

		static ModMonoWorkerRequest ()
		{
			string indexes = ConfigurationSettings.AppSettings ["MonoServerDefaultIndexFiles"];
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
			this.protocol = protocol;
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
			} else if (path [path.Length - 1] == '/') {
				path = path + indexFiles [0];
			}

			// Yes, MS only looks for the '.'. Try setting a handler
			// for something not containing a '.' and you won't get
			// path_info
			int dot = path.IndexOf ('.');
			int slash = (dot != -1) ? path.IndexOf ('/', dot) : -1;
			if (dot >= 0 && slash >= 0) {
				pathInfo = path.Substring (slash);
				path = path.Substring (0, slash);
			} else {
				pathInfo = "";
			}

			return true;
		}
		
		public override bool HeadersSent ()
		{
			//FIXME!!!!: how do we know this?
			return false;
		}
		
		public override void FlushResponse (bool finalFlush)
		{
			requestBroker.Flush (requestId);
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

		public override string GetServerVariable (string name)
		{
			return requestBroker.GetServerVariable (requestId, name);
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
			//TODO
			return true;
		}

		public override string GetUriPath ()
		{
			WebTrace.WriteLine ("GetUriPath()");

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
			IHashCodeProvider hp = CaseInsensitiveHashCodeProvider.Default;
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
	}
}

