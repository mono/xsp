//
// Mono.WebServer.XSPWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Simon Waite (simon@psionics.demon.co.uk)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2005 Novell, Inc. (http://www.novell.com)
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Runtime.InteropServices;

namespace Mono.WebServer
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
		byte [] inputBuffer;
		int inputLength;
		bool refilled;
		int position;
		EndPoint remoteEP;
		bool sentConnection;
		int localPort;
		string localAddress;
		int requestId;
		XSPRequestBroker requestBroker;
		bool keepAlive;
		bool haveContentLength;
		IntPtr socket;
		bool secure;

		static bool running_tests;
		static bool no_libc;
		static string server_software;
		static string serverHeader;

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

			int platform = (int) Environment.OSVersion.Platform;
			string plat;
			if (platform == 4 || platform == 128)
				plat = "Unix";
			else
				plat = ((PlatformID) platform).ToString ();

			server_software = String.Format ("{0}/{1}", title, version); 
			serverHeader = String.Format ("Server: {0} {1}\r\n", server_software, plat);

#if NET_2_0
			string indexes = ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"];
#else
			string indexes = ConfigurationSettings.AppSettings ["MonoServerDefaultIndexFiles"];
#endif
			SetDefaultIndexFiles (indexes);
		}

		static bool CheckOS ()
		{
			if (Environment.GetEnvironmentVariable ("XSP_NO_LIBC") != null)
				return true;

			bool is_linux = false;
			try {
				string os = "";
				using (Stream st = File.OpenRead ("/proc/sys/kernel/ostype")) {
					StreamReader sr = new StreamReader (st);
					os = sr.ReadToEnd ();
				}
				is_linux = os.StartsWith ("Linux");
			} catch {
			}

			return !is_linux;
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
		
		public XSPWorkerRequest (int requestId, 
								XSPRequestBroker requestBroker, 
								IApplicationHost appHost, 
								EndPoint localEP,
					 			EndPoint remoteEP, 
								string verb, 
								string path, 
								string queryString, 
								string protocol, 
								byte[] inputBuffer,
								IntPtr socket,
								bool secure)
			: base (appHost)
		{
			this.socket = socket;
			this.requestId = requestId;
			this.requestBroker = requestBroker;
			this.remoteEP = remoteEP;
			this.verb = verb;
			Paths.GetPathsFromUri (path, out this.path, out pathInfo);
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

			string cncHeader = (string) headers ["Connection"];
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
			responseHeaders.Append (serverHeader);
			status = protocol + " 200 OK\r\n";
			
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
			StringBuilder text = new StringBuilder ();
			bool cr = false;
			do {
				if (inputBuffer == null || position >= inputLength)
					FillBuffer ();

				if (position >= inputLength)
					break;
				
				cr = false;
				int count = 0;
				byte b = 0;
				int i;
				for (i = position; count < 8192 && i < inputLength; i++, count++) {
					b = inputBuffer [i];
					if (b == '\r') {
						cr = true;
						count--;
						continue;
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
				}
				break;
			} while (true);

			if (text.Length == 0)
				return null;

			return text.ToString ();
		}

		void GetRequestHeaders ()
		{
			try {
				string line;
				headers = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant,
							CaseInsensitiveComparer.DefaultInvariant);
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
			if (requestBroker != null) {
				// We check for headersSent as broken user code might call
				// CloseConnection at an early stage.
				requestBroker.Close (requestId, (headersSent ? keepAlive : false));
				requestBroker = null;
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

		byte [] GetHeaders ()
		{
			responseHeaders.Insert (0, status);
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

			string headerName = HttpWorkerRequest.GetKnownRequestHeaderName (index);
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

			byte [] result = new byte [inputLength - position];
			Buffer.BlockCopy (inputBuffer, position, result, 0, inputLength - position);
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
			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			if (queryString != null && queryString.Length > 0)
				return result + "?" + queryString;

			return result;
		}

		public override string GetRemoteAddress ()
		{
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
			return ((IPEndPoint) remoteEP).Port;
		}


		public override string GetServerVariable (string name)
		{
			string result = null;
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
			if (pathInfo != null && pathInfo.Length > 0)
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

			string content_length = (string) headers ["Content-Length"];
			long length = -1;
			try {
				length = Int64.Parse (content_length);
			} catch {
				return false;
			}

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

			Send (data, 0, length);
			if (uncork)
				Cork (false);
		}
		
		public override void SendStatus (int statusCode, string statusDescription)
		{
			status = String.Format ("{2} {0} {1}\r\n", statusCode, statusDescription, protocol);
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

			if (!headersSent) {
				responseHeaders.Append (name);
				responseHeaders.Append (": ");
				responseHeaders.Append (value);
				responseHeaders.Append ("\r\n");
			}
		}

 		public override bool IsSecure ()
 		{
 			return secure;
 		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			if (secure || no_libc || (tried_sendfile && !use_sendfile)) {
				base.SendResponseFromFile (handle, offset, length);
				return;
			}

			int result;
			try {
				tried_sendfile = true;
				Cork (true);
				SendHeaders ();
				while (length > 0) {
					result = sendfile (socket, handle, ref offset, (IntPtr) length);
					if (result == -1)
						throw new System.ComponentModel.Win32Exception ();

					// sendfile() will set 'offset' for us
					length -= result;
				}
			} finally {
				Cork (false);
			}

			use_sendfile = true;
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
			return setsockopt (socket, (IntPtr) 6, (IntPtr) 3, ref t, (IntPtr) IntPtr.Size);
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
					int n = send (socket, ptr + total, (IntPtr) (len - total), (IntPtr) 0x4000);
					if (n >= 0) {
						total += n;
					} else {
						throw new IOException ();
					}
				}
			}

			return total;
		}

		static bool tried_sendfile;
		static bool use_sendfile;

		[DllImport ("libc", SetLastError=true)]
		extern static int setsockopt (IntPtr handle, IntPtr level, IntPtr opt, ref bool val, IntPtr len);

		[DllImport ("libc", SetLastError=true)]
		extern static int sendfile (IntPtr out_fd, IntPtr in_fd, ref long offset, IntPtr count);

		[DllImport ("libc", SetLastError=true, EntryPoint="send")]
		unsafe extern static int send (IntPtr s, byte *buffer, IntPtr len, IntPtr flags);
	}
}

