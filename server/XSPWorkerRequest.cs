//
// Mono.ASPNET.XSPWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Simon Waite (simon@psionics.demon.co.uk)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	public class XSPWorkerRequest : MonoWorkerRequest
	{
		string verb;
		string path;
		string pathInfo;
		string queryString;
		string protocol;
		Hashtable headers;
		string [][] unknownHeaders;
		bool headersSent;
		StringBuilder responseHeaders;
		string status;
		MemoryStream response;
		byte [] inputBuffer;
		int inputLength;
		int position;
		EndPoint remoteEP;
		bool sentConnection;
		int localPort;
		string localAddress;
		int requestId;
		XSPRequestBroker requestBroker;
		bool keepAlive;
		bool haveContentLength;
		long contentSent;
		long contentLength;
		bool isclosed;
		
		static string serverHeader;

		static string [] indexFiles = { "index.aspx",
						"Default.aspx",
						"default.aspx",
						"index.html",
						"index.htm" };
						

		static XSPWorkerRequest ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string title = "Mono-XSP Server";
			string version = assembly.GetName ().Version.ToString ();
			object [] att = assembly.GetCustomAttributes (typeof (AssemblyTitleAttribute), false);
			if (att.Length > 0)
				title = ((AssemblyTitleAttribute) att [0]).Title;

			string plat = Environment.OSVersion.Platform.ToString ();
			if (plat == "128")
				plat = "Unix";

			serverHeader = String.Format ("Server: {0}/{1} {2}\r\n",
						      title, version, plat); 

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
		
		static Stack bufferStack = new Stack ();
		
		static MemoryStream AllocateMemoryStream ()
		{
			lock (bufferStack) {
				if (bufferStack.Count != 0)
					return (MemoryStream) bufferStack.Pop ();
			}
			return new MemoryStream ();
		}
		
		static void FreeMemoryStream (MemoryStream buf)
		{
			buf.SetLength (0);
			lock (bufferStack) {
				bufferStack.Push (buf);
			}
		}
		
		public XSPWorkerRequest (int requestId, 
								XSPRequestBroker requestBroker, 
								IApplicationHost appHost, 
								EndPoint localEP,
					 			EndPoint remoteEP, 
								string verb, 
								string path, 
								string pathInfo, 
								string queryString, 
								string protocol, 
								byte[] inputBuffer)
			: base (appHost)
		{
			this.requestId = requestId;
			this.requestBroker = requestBroker;
			this.remoteEP = remoteEP;
			this.verb = verb;
			this.path = path;
			this.pathInfo = pathInfo;
			this.protocol = protocol;
			if (protocol == "HTTP/1.1") {
				this.protocol = "HTTP/1.0";	// Only 1.0 supported by xsp standalone.
				keepAlive = true;
			}

			this.queryString = queryString;
			this.inputBuffer = inputBuffer;
			inputLength = inputBuffer.Length;
			position = 0;

			GetRequestHeaders ();
			string cncHeader = (string) headers ["Connection"];
			if (cncHeader != null) {
				cncHeader = cncHeader.ToLower ();
				if (cncHeader.IndexOf ("keep-alive") != -1)
					keepAlive = true;

				if (cncHeader.IndexOf ("close") != -1)
					keepAlive = false;
			}

			responseHeaders = new StringBuilder ();
			responseHeaders.Append (serverHeader);
			response = AllocateMemoryStream ();
			status = "HTTP/1.0 200 OK\r\n";
			
			localPort = ((IPEndPoint) localEP).Port;
			localAddress = ((IPEndPoint) localEP).Address.ToString();
		}
		
		public override int RequestId {
			get { return requestId; }
		}

		void FillBuffer ()
		{
			inputLength = requestBroker.Read (requestId, 32*1024, out inputBuffer);
			position = 0;
		}

		int ReadInputByte ()
		{
			if (inputBuffer == null || position >= inputLength)
				FillBuffer ();

			return (int) inputBuffer [position++];
		}

		string ReadLine ()
		{
			bool foundCR = false;
			StringBuilder text = new StringBuilder ();

			while (true) {
				int c = ReadInputByte ();

				if (c == -1) {				// end of stream
					if (text.Length == 0)
						return null;

					if (foundCR)
						text.Length--;

					break;
				}

				if (c == '\n') {			// newline
					if ((text.Length > 0) && (text [text.Length - 1] == '\r'))
						text.Length--;

					foundCR = false;
					break;
				} else if (foundCR) {
					text.Length--;
					break;
				}

				if (c == '\r')
					foundCR = true;
					

				text.Append ((char) c);
				if (text.Length > 8192)
					throw new InvalidOperationException ("Line too long.");
			}

			return text.ToString ();
		}

		void GetRequestHeaders ()
		{
			try {
				string line;
				headers = new Hashtable (CaseInsensitiveHashCodeProvider.Default,
							CaseInsensitiveComparer.Default);
				while ((line = ReadLine ()) != null && line.Length > 0) {
					int colon = line.IndexOf (':');
					if (colon == -1 || line.Length < colon + 2)
						throw new Exception ();
					string key = line.Substring (0, colon);
					string value = line.Substring (colon + 1).Trim ();
					headers [key] = value;
				}
			} catch (IOException ioe) {
				throw;
			} catch (Exception e) {
				throw new Exception ("Error reading headers.", e);
			}
		}

		public override void CloseConnection ()
		{
			WebTrace.WriteLine ("CloseConnection()");
			if (requestBroker != null) {
				// We check for headersSent as broken user code might call
				// CloseConnection at an early stage.
				requestBroker.Close (requestId, (headersSent ? keepAlive : false));
				requestBroker = null;
				FreeMemoryStream (response);
				response = null;
			}
		}

		void AddConnectionHeader ()
		{
			if (!keepAlive) {
				responseHeaders.Append ("Connection: close\r\n");
				return;
			}

			int allowed = requestBroker.GetReuseCount (requestId);
			if (allowed <= 0) {
				keepAlive = false;
				responseHeaders.Append ("Connection: close\r\n");
				return;
			}

			responseHeaders.Append ("Keep-Alive: timeout=15, max=");
			responseHeaders.Append (allowed.ToString ());
			responseHeaders.Append ("\r\n");
			responseHeaders.Append ("Connection: Keep-Alive\r\n");
		}

		int UpdateBodyLength (int currentBlockLength)
		{
			if (!haveContentLength || contentSent < contentLength - currentBlockLength) {
				contentSent += currentBlockLength;
				return currentBlockLength;
			}

			int result = (int) (contentLength - contentSent);
			contentSent = contentLength;
			return result;
		}

		public override void FlushResponse (bool finalFlush)
		{
			if (requestBroker == null)
				return;

			try {
				if (!headersSent) {
					responseHeaders.Insert (0, status);
					if (!sentConnection) {
						if (!haveContentLength)
							keepAlive = false;

						AddConnectionHeader ();
					}

					responseHeaders.Append ("\r\n");
					byte [] headerBytes = Encoding.GetBytes (responseHeaders.ToString ());
					int oldLength = (int) response.Length;
					if (oldLength == 0 || oldLength >= 32768) {
						requestBroker.Write (requestId, headerBytes, 0, headerBytes.Length);
					} else {
						oldLength = UpdateBodyLength (oldLength);
						// Attempt not to send a minimum of 2 packets
						int newLength = oldLength + headerBytes.Length;
						response.SetLength (newLength);
						byte [] buf = response.GetBuffer ();
						Buffer.BlockCopy (buf, 0, buf, headerBytes.Length, oldLength);
						Buffer.BlockCopy (headerBytes, 0, buf, 0, headerBytes.Length);
						requestBroker.Write (requestId, buf, 0, newLength);
						response.SetLength (0);
					}

					headersSent = true;
				}

				if (response.Length != 0) {
					byte [] bytes = response.GetBuffer ();
					int len = UpdateBodyLength ((int) response.Length);
					requestBroker.Write (requestId, bytes, 0, len);
				}
				
				if (finalFlush)
					CloseConnection ();
				else {
					requestBroker.Flush (requestId);
					response.SetLength (0);
				}
			} catch (Exception e) {
				WebTrace.WriteLine (e.ToString ());
				CloseConnection ();
			}
		}

		public override string GetFilePath ()
		{
			WebTrace.WriteLine ("GetFilePath()");
			return path;
		}

		public override string GetHttpVerbName ()
		{
			WebTrace.WriteLine ("GetHttpVerbName()");
			return verb;
		}

		public override string GetHttpVersion ()
		{
			WebTrace.WriteLine ("GetHttpVersion()");
			return protocol;
		}

		public override string GetKnownRequestHeader (int index)
		{
			if (headers == null)
				return null;

			string headerName = HttpWorkerRequest.GetKnownRequestHeaderName (index);
			WebTrace.WriteLine (String.Format ("GetKnownRequestHeader({0}) -> {1}", index, headerName));
			return headers [headerName] as string;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			if (headers == null)
				return null;

			return headers [name] as string;
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			if (unknownHeaders == null) {
				if (headers == null)
					return (unknownHeaders = new string [0][]);

				ICollection keysColl = headers.Keys;
				ICollection valuesColl = headers.Values;
				string [] keys = new string [keysColl.Count];
				string [] values = new string [valuesColl.Count];
				keysColl.CopyTo (keys, 0);
				valuesColl.CopyTo (values, 0);

				int count = keys.Length;
				ArrayList pairs = new ArrayList ();
				for (int i = 0; i < count; i++) {
					int index = HttpWorkerRequest.GetKnownRequestHeaderIndex (keys [i]);
					if (index != -1)
						continue;
					pairs.Add (new string [] { keys [i], values [i]});
				}
				
				if (pairs.Count != 0) {
					unknownHeaders = new string [pairs.Count][];
					for (int i = 0; i < pairs.Count; i++)
						unknownHeaders [i] = (string []) pairs [i];
					//unknownHeaders = (string [][]) pairs.ToArray (typeof (string [][]));
				}
			}

			return unknownHeaders;
		}

		public override string GetLocalAddress ()
		{
			WebTrace.WriteLine ("GetLocalAddress()");
			return localAddress;
		}

		public override int GetLocalPort ()
		{
			WebTrace.WriteLine ("GetLocalPort()");
			return localPort;
		}

		public override string GetPathInfo ()
		{
			WebTrace.WriteLine ("GetPathInfo()");
			return pathInfo;
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			WebTrace.WriteLine ("GetPreloadedEntityBody");
			return null;
		}

		public override string GetQueryString ()
		{
			WebTrace.WriteLine ("GetQueryString()");
			return queryString;
		}

		public override byte [] GetQueryStringRawBytes ()
		{
			WebTrace.WriteLine ("GetQueryStringRawBytes()");
			if (queryString == null)
				return null;
			return Encoding.GetBytes (queryString);
		}

		public override string GetRawUrl ()
		{
			WebTrace.WriteLine ("GetRawUrl()");
			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			if (queryString != null && queryString.Length > 0)
				return result + "?" + queryString;

			return result;
		}

		public override string GetRemoteAddress ()
		{
			WebTrace.WriteLine ("GetRemoteAddress()");
			return ((IPEndPoint) remoteEP).Address.ToString ();
		}

		public override string GetRemoteName ()
		{
			string ip = GetRemoteAddress ();
			string name = null;
			try {
				IPHostEntry entry = Dns.GetHostByName (ip);
				name = entry.HostName;
			} catch {
				name = ip;
			}

			return name;
		}
		
		public override int GetRemotePort ()
		{
			WebTrace.WriteLine ("GetRemotePort()");
			return ((IPEndPoint) remoteEP).Port;
		}


		public override string GetServerVariable (string name)
		{
			WebTrace.WriteLine ("GetServerVariable()");
			return "GetServerVariable -> " + name;
		}

		public override string GetUriPath ()
		{
			WebTrace.WriteLine ("GetUriPath()");

			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			return result;
		}

		public override bool HeadersSent ()
		{
			WebTrace.WriteLine ("HeadersSent() -> " + headersSent);
			return headersSent;
		}

		public override bool IsClientConnected ()
		{
			WebTrace.WriteLine ("IsClientConnected()");
			return (requestBroker != null && requestBroker.IsConnected (requestId));
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return false; //TODO: handle preloading data
		}

		bool TryDirectory ()
		{
			string localPath = GetFilePathTranslated ();
			
			if (!Directory.Exists (localPath))
				return true;

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
			return TryDirectory ();
		}

		int ReadInput (byte [] buffer, int offset, int size)
		{
			int length = inputLength - position;
			if (length > 0) {
				if (length > size)
					length = size;

				Buffer.BlockCopy (inputBuffer, position, buffer, offset, length);
				position += length;
				offset += length;
				size -= length;
				if (size == 0)
					return length;
			}

			int localsize = size;
			while (localsize > 0) {
				byte[] readBuffer;
				int read = requestBroker.Read (requestId, localsize, out readBuffer);
				Array.Copy (readBuffer, 0, buffer, offset, read);
				offset += read;
				localsize -= read;
			}

			return (length + size);
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			WebTrace.WriteLine ("ReadEntityBody()");
			if (size == 0)
				return 0;

			return ReadInput (buffer, 0, size);
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			WebTrace.WriteLine ("SendResponseFromMemory ()");
			if (requestBroker == null || length <= 0)
				return;

			if (data.Length < length)
				length = data.Length;

			response.Write (data, 0, length);
		}
		
		public override void SendStatus (int statusCode, string statusDescription)
		{
			status = String.Format ("HTTP/1.0 {0} {1}\r\n", statusCode, statusDescription);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			WebTrace.WriteLine ("SendUnknownResponseHeader (" + name + ", " + value + ")");
			if (String.Compare (name, "connection", true, CultureInfo.InvariantCulture) == 0) {
				sentConnection = true;
				if (value.ToLower ().IndexOf ("keep-alive") == -1) {
					keepAlive = false;
				}
			}

			if (!sentConnection && !haveContentLength &&
			     String.Compare (name, "Content-Length", true, CultureInfo.InvariantCulture) == 0) {
				haveContentLength = true;
				contentLength = Int64.Parse (value); // This should work, otherwise HttpResponse throws.
			}

			if (!headersSent) {
				responseHeaders.Append (name);
				responseHeaders.Append (": ");
				responseHeaders.Append (value);
				responseHeaders.Append ("\r\n");
			}
		}
	}
}

