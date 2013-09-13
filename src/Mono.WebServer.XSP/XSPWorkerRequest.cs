//
// Mono.WebServer.XSPWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Simon Waite (simon@psionics.demon.co.uk)
//	Marek Habersack (grendel@twistedcode.net)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2005 Novell, Inc. (http://www.novell.com)
// Copyright 2012 Xamarin, Inc (http://xamarin.com)
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
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Runtime.InteropServices;
using Mono.WebServer.Log;

namespace Mono.WebServer
{
	public class XSPWorkerRequest : MonoWorkerRequest
	{
		readonly string verb;
		string path;
		readonly string rawUrl;
		readonly string pathInfo;
		readonly string queryString;
		readonly string protocol;
		Hashtable headers;
		string [][] unknownHeaders;
		bool headersSent;
		readonly StringBuilder responseHeaders;
		int statusCode;
		string statusDescription;
		byte [] inputBuffer;
		int inputLength;
		bool refilled;
		int position;
		readonly EndPoint remoteEP;
		bool sentConnection;
		readonly int localPort;
		readonly string localAddress;
		readonly int requestId;
		XSPRequestBroker requestBroker;
		bool keepAlive;
		bool haveContentLength;
		readonly IntPtr socket;
		readonly bool secure;

		static readonly bool running_tests;
		static readonly bool no_libc;
		static readonly string server_software;
		static readonly string serverHeader;

		static string [] indexFiles = { "index.aspx",
						"Default.aspx",
						"default.aspx",
						"index.html",
						"index.htm" };
						

		static XSPWorkerRequest ()
		{
			no_libc = CheckOS ();
			running_tests = (Environment.GetEnvironmentVariable ("XSP_RUNNING_TESTS") != null);
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string title = "Mono-XSP Server";
			string version = assembly.GetName ().Version.ToString ();
			object [] att = assembly.GetCustomAttributes (typeof (AssemblyTitleAttribute), false);
			if (att.Length > 0)
				title = ((AssemblyTitleAttribute) att [0]).Title;

			server_software = String.Format ("{0}/{1}", title, version); 
			serverHeader = String.Format ("\r\nServer: {0} {1}\r\n", server_software, Platform.Name);

			
			try {
				string indexes = ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"];

				SetDefaultIndexFiles (indexes);
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Worker initialization exception occurred. Continuing anyway:");
				Logger.Write (ex);
			}
		}

		static bool CheckOS ()
		{
			if (Environment.GetEnvironmentVariable ("XSP_NO_LIBC") != null)
				return true;

			bool is_linux = false;
			try {
				string os = File.ReadAllText ("/proc/sys/kernel/ostype");
				is_linux = os.StartsWith ("Linux");
			} catch {
			}

			return !is_linux;
		}

		static void SetDefaultIndexFiles (string list)
		{
			if (list == null)
				return;

			indexFiles = SplitAndTrim (list);
		}

		public XSPWorkerRequest (int requestId,
		                         XSPRequestBroker requestBroker,
		                         IApplicationHost appHost,
		                         EndPoint localEP, EndPoint remoteEP,
		                         string verb, string path,
		                         string queryString, string protocol,
		                         byte[] inputBuffer, IntPtr socket,
		                         bool secure)
			: base (appHost)
		{
			this.socket = socket;
			this.requestId = requestId;
			this.requestBroker = requestBroker;
			this.remoteEP = remoteEP;
			this.verb = verb;
			rawUrl = path;
			if (!String.IsNullOrEmpty (queryString))
				rawUrl += "?" + queryString;
			try {
				Paths.GetPathsFromUri (appHost, verb, path, out this.path, out pathInfo);
			} catch {
				CloseConnection ();
				throw;
			}
			
			this.protocol = protocol;
			if (protocol == "HTTP/1.1") {
				if (!running_tests)
					this.protocol = "HTTP/1.0";
				keepAlive = true;
			}

			this.queryString = queryString;
			this.inputBuffer = inputBuffer;
			inputLength = inputBuffer.Length;
			position = 0;
			this.secure = secure;

			try {
				GetRequestHeaders ();
			} catch {
				CloseConnection ();
				throw;
			}

			var cncHeader = (string) headers ["Connection"];
			if (cncHeader != null) {
				cncHeader = cncHeader.ToLower ();
				if (cncHeader.IndexOf ("keep-alive") != -1)
					keepAlive = true;

				if (cncHeader.IndexOf ("close") != -1)
					keepAlive = false;
			}

			if (secure)
				keepAlive = false; //FIXME: until the NetworkStream don't own the socket for ssl streams. 

			responseHeaders = new StringBuilder ();
			statusCode = 200;
			statusDescription = "OK";
			
			localPort = ((IPEndPoint) localEP).Port;
			localAddress = ((IPEndPoint) localEP).Address.ToString();
		}
		
		public override int RequestId {
			get { return requestId; }
		}

		void FillBuffer ()
		{
			refilled = true;
			position = 0;
			inputLength = requestBroker.Read (requestId, 32*1024, out inputBuffer);
		}

		string ReadLine ()
		{
			var text = new StringBuilder ();
			do {
				if (inputBuffer == null || position >= inputLength)
					FillBuffer ();

				if (position >= inputLength)
					break;
				
				bool cr = false;
				int count = 0;
				byte b = 0;
				int i;
				for (i = position; count < 8192 && i < inputLength; i++, count++) {
					b = inputBuffer [i];
					if (b == '\r') {
						cr = true;
						count--;
					} else if (b == '\n' || cr) {
						count--;
						break;
					}
				}

				if (position >= inputLength && b == '\r' || b == '\n')
					count++;

				if (count >= 8192 || count + text.Length >= 8192)
					throw new InvalidOperationException ("Line too long.");

				if (count <= 0) {
					position = i + 1;
					break;
				}

				text.Append (Encoding.GetString (inputBuffer, position, count));
				position = i + 1;

				if (i >= inputLength) {
					b = inputBuffer [inputLength - 1];
					if (b != '\r' && b != '\n')
						continue;
					FillBuffer();
					if (b == '\r' && inputLength > 0
						&& inputBuffer[0] == '\n')
						position++;
				}
				break;
			} while (true);

			return text.Length == 0 ? null : text.ToString ();
		}

		void GetRequestHeaders ()
		{
			try {
				string line;
				headers = new Hashtable (StringComparer.InvariantCultureIgnoreCase);
				while ((line = ReadLine ()) != null && line.Length > 0) {
					int colon = line.IndexOf (':');
					if (colon == -1 || line.Length < colon + 2)
						throw new Exception ();
					string key = line.Substring (0, colon);
					string value = line.Substring (colon + 1).Trim ();
					headers [key] = value;
				}
			} catch (IOException) {
				throw;
			} catch (Exception e) {
				throw new Exception ("Error reading headers.", e);
			}
		}

		public override void CloseConnection ()
		{
			if (requestBroker == null)
				return;
			// We check for headersSent as broken user code might call
			// CloseConnection at an early stage.
			requestBroker.Close (requestId, headersSent && keepAlive);
			requestBroker = null;
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

		byte [] GetHeaders ()
		{
			var basicHeaders = new StringBuilder();
			basicHeaders.Append (protocol);
			if (statusCode == 200)
				basicHeaders.Append (" 200 ");
			else {
				basicHeaders.Append (' ');
				basicHeaders.Append (statusCode.ToString (CultureInfo.InvariantCulture));
				basicHeaders.Append (' ');
			}
			basicHeaders.Append (statusDescription);
			basicHeaders.Append ("\r\nDate: ");
			basicHeaders.Append (DateTime.UtcNow.ToString ("r", CultureInfo.InvariantCulture));
			basicHeaders.Append (serverHeader);
			responseHeaders.Insert (0, basicHeaders.ToString ());

			if (!sentConnection) {
				if (!haveContentLength)
					keepAlive = false;

				AddConnectionHeader ();
			}

			responseHeaders.Append ("\r\n");
			return HeaderEncoding.GetBytes (responseHeaders.ToString ());
		}

		public override void FlushResponse (bool finalFlush)
		{
			try {
				if (!headersSent)
					SendHeaders ();

				if (finalFlush)
					CloseConnection ();
			} catch (Exception) {
				CloseConnection ();
			}
		}

		public override string GetFilePath ()
		{
			return path;
		}

		public override string GetHttpVerbName ()
		{
			return verb;
		}

		public override string GetHttpVersion ()
		{
			return protocol;
		}

		public override string GetKnownRequestHeader (int index)
		{
			if (headers == null)
				return null;

			string headerName = GetKnownRequestHeaderName (index);
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
				var keys = new string [keysColl.Count];
				var values = new string [valuesColl.Count];
				keysColl.CopyTo (keys, 0);
				valuesColl.CopyTo (values, 0);

				int count = keys.Length;
				var pairs = new List<string[]> ();
				for (int i = 0; i < count; i++) {
					int index = GetKnownRequestHeaderIndex (keys [i]);
					if (index != -1)
						continue;
					pairs.Add (new[] { keys [i], values [i]});
				}
				
				if (pairs.Count != 0)
					unknownHeaders = pairs.ToArray ();
			}

			return unknownHeaders;
		}

		public override string GetLocalAddress ()
		{
			return localAddress;
		}

		public override int GetLocalPort ()
		{
			return localPort;
		}

		public override string GetPathInfo ()
		{
			return pathInfo;
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			if (verb != "POST" || refilled || position >= inputLength)
				return null;

			if (inputLength == 0)
				return null;

			var content_length = (string) headers ["Content-Length"];
			long length = 0;

			// If not empty parse, if correctly parsed validate
			if (!String.IsNullOrEmpty (content_length)
				&& Int64.TryParse (content_length, out length)
				&& length > Int32.MaxValue)
				throw new InvalidOperationException ("Content-Length exceeds the maximum accepted size.");

			int input_data_length = inputLength - position;
			if (length < 0 || length > input_data_length)
				length = input_data_length;

			var result = new byte [length];
			Buffer.BlockCopy (inputBuffer, position, result, 0, (int)length);
			position = 0;
			inputLength = 0;
			inputBuffer = null;
			return result;
		}

		public override string GetQueryString ()
		{
			return queryString;
		}

		public override byte [] GetQueryStringRawBytes ()
		{
			if (queryString == null)
				return null;
			return Encoding.GetBytes (queryString);
		}

		public override string GetRawUrl ()
		{
			return rawUrl;
		}

		public override string GetRemoteAddress ()
		{
			return ((IPEndPoint) remoteEP).Address.ToString ();
		}

		public override string GetRemoteName ()
		{
			string ip = GetRemoteAddress ();
			string name;
			try {
				IPHostEntry entry = Dns.GetHostEntry (ip);
				name = entry.HostName;
			} catch {
				name = ip;
			}

			return name;
		}
		
		public override int GetRemotePort ()
		{
			return ((IPEndPoint) remoteEP).Port;
		}


		public override string GetServerVariable (string name)
		{
			string result;
			switch (name) {
			case "GATEWAY_INTERFACE":
				result = "CGI/1.1";
				break;
			case "HTTPS":
				result = (IsSecure ()) ? "on" : "off";
				break;
			case "SERVER_SOFTWARE":
				result = server_software;
				break;
			default:
				result = base.GetServerVariable (name);
				break;
			}

			return result;
		}

		public override string GetUriPath ()
		{
			string result = path;
			if (!String.IsNullOrEmpty (pathInfo))
				result += pathInfo;

			return result;
		}

		public override bool HeadersSent ()
		{
			return headersSent;
		}

		public override bool IsClientConnected ()
		{
			return (requestBroker != null && requestBroker.IsConnected (requestId));
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			if (verb != "POST" || refilled || position >= inputLength)
				return false;

			var content_length = (string) headers ["Content-Length"];
			long length;
			if (!Int64.TryParse (content_length, out length))
				return false;

			return (length <= inputLength);
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

			int localsize = 0;
			while (size > 0) {
				byte[] readBuffer;
				int read = requestBroker.Read (requestId, size, out readBuffer);
				if (read == 0)
					break;

				if (read < 0)
					throw new HttpException (500, "Error reading request.");
				Buffer.BlockCopy (readBuffer, 0, buffer, offset, read);
				offset += read;
				size -= read;
				localsize += read;
			}

			return (length + localsize);
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			if (verb == "GET" || verb == "HEAD" || size == 0 || buffer == null)
				return 0;

			return ReadInput (buffer, 0, size);
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			if (requestBroker == null || length <= 0)
				return;

			if (data.Length < length)
				length = data.Length;

			bool uncork = false;
			if (!headersSent) {
				Cork (true);
				uncork = true;
				SendHeaders ();
			}

			int sent = Send (data, 0, length);
			if (sent != length)
				throw new IOException ("Blocking send did not send entire buffer");

			if (uncork)
				Cork (false);
		}
		
		public override void SendStatus (int statusCode, string statusDescription)
		{
			this.statusCode = statusCode;
			this.statusDescription = statusDescription;
			if (statusCode == 400 || statusCode >= 500) {
				sentConnection = false;
				keepAlive = false;
				SendUnknownResponseHeader ("Connection", "close");
			}
				
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			if (String.Compare (name, "connection", true, CultureInfo.InvariantCulture) == 0) {
				sentConnection = true;
				if (value.ToLower ().IndexOf ("keep-alive") == -1) {
					keepAlive = false;
				}
			}

			if (!sentConnection && !haveContentLength &&
				String.Compare (name, "Content-Length", true, CultureInfo.InvariantCulture) == 0) {
				haveContentLength = true;
			}

			if (headersSent)
				return;

			responseHeaders.Append (name);
			responseHeaders.Append (": ");
			responseHeaders.Append (value);
			responseHeaders.Append ("\r\n");
		}

 		public override bool IsSecure ()
 		{
 			return secure;
 		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			using (FileStream fs = File.OpenRead (filename)) {
				if (secure || no_libc) {
					// We must not call the SendResponseFromFile overload which
					// takes  IntPtr in this case since it will call the base
					// implementation of that overload which, in turn, will
					// close the handle (as it uses FileStream to wrap the
					// handle we pass). This will cause the handle to be closed
					// twice (FileStream owns the handle). So we just take a
					// shortcut to what the base overload does here.
					SendFromStream (fs, offset, length);
				} else {
					SendResponseFromFile (fs.Handle, offset, length);
				}
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			if (secure || no_libc) {
				base.SendResponseFromFile (handle, offset, length);
				return;
			}

			try {
				Cork (true);
				SendHeaders ();
				while (length > 0) {
					int result = sendfile ((int) socket, (int) handle, ref offset, (IntPtr) length);
					if (result == -1)
						throw new System.ComponentModel.Win32Exception ();

					// sendfile() will set 'offset' for us
					length -= result;
				}
			} finally {
				Cork (false);
			}
		}

		int SendHeaders ()
		{
			if (headersSent)
				return 0;

			byte [] headers = GetHeaders ();
			headersSent = true;
			return Send (headers, 0, headers.Length);
		}

		int Cork (bool val)
		{
			if (secure || no_libc)
				return 0;
			// 6 -> SOL_TCP, 3 -> TCP_CORK
			bool t = val;
			return setsockopt ((int) socket, 6, 3, ref t, (IntPtr) IntPtr.Size);
		}

		unsafe int Send (byte [] buffer, int offset, int len)
		{
			if (secure || no_libc) {
				requestBroker.Write (requestId, buffer, offset, len);
				return len;
			}

			int total = 0;
			while (total < len) {
				fixed (byte *ptr = buffer) {
					// 0x4000 no sigpipe
					int n = send ((int) socket, ptr + total, (IntPtr) (len - total), 0x4000);
					if (n >= 0) {
						total += n;
					} else if (Marshal.GetLastWin32Error () != 4 /* EINTR */) {
						throw new IOException ();
					}
				}
			}

			return total;
		}

		[DllImport ("libc", SetLastError=true)]
		extern static int setsockopt (int handle, int level, int opt, ref bool val, IntPtr len);

		[DllImport ("libc", SetLastError=true)]
		extern static int sendfile (int out_fd, int in_fd, ref long offset, IntPtr count);

		[DllImport ("libc", SetLastError=true, EntryPoint="send")]
		unsafe extern static int send (int s, byte *buffer, IntPtr len, int flags);
	}
}

