//
// Mono.ASPNET.XSPWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Simon Waite (simon@psionics.demon.co.uk)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
//
using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
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
		IApplicationHost appHost;
		Stream stream;
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
		EndPoint localEP;
		EndPoint remoteEP;
		bool sentConnection;
		int localPort;
		string localAddress;
		
		static byte [] error500;

		static string serverHeader;

		static string dirSeparatorString = Path.DirectorySeparatorChar.ToString ();

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

			serverHeader = String.Format ("Server: {0}/{1} {2}\r\n",
						      title, version, Environment.OSVersion.Platform);

			string indexes = ConfigurationSettings.AppSettings ["MonoServerDefaultIndexFiles"];
			SetDefaultIndexFiles (indexes);

			string s = "HTTP/1.0 500 Server error\r\n" +
				   serverHeader + 
				    "<html><body><h1>500 Server error</h1>\r\n" +
				   "Your client sent a request that was not understood by this server.\r\n" +
				   "</body></html>\r\n";
			
			error500 = Encoding.Default.GetBytes (s);
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

		public XSPWorkerRequest (NetworkStream ns, IApplicationHost appHost, EndPoint localEP,
					 EndPoint remoteEP, RequestData rdata)
			: base (appHost)
		{
			if (ns == null)
				throw new ArgumentNullException ("ns");

			this.appHost = appHost;
			this.localEP = localEP;
			this.remoteEP = remoteEP;
			stream = ns;
			verb = rdata.Verb;
			path = rdata.Path;
			pathInfo = rdata.PathInfo;
			protocol = rdata.Protocol;
			if (protocol == "HTTP/1.1")
				protocol = "HTTP/1.0";	// Only 1.0 supported by xsp standalone.

			queryString = rdata.QueryString;
			inputBuffer = rdata.InputBuffer;
			inputLength = inputBuffer.Length;
			position = 0;

			GetRequestHeaders ();
			responseHeaders = new StringBuilder ();
			response = new MemoryStream ();
			status = "HTTP/1.0 200 OK\r\n";
			
			localPort = ((IPEndPoint) localEP).Port;
			localAddress = ((IPEndPoint) localEP).Address.ToString();
		}

		void FillBuffer ()
		{
			inputBuffer = new byte [32*1024];
			inputLength = stream.Read (inputBuffer, 0, 32*1024);
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
			}

			return text.ToString ();
		}

		void GetRequestHeaders ()
		{
			try {
				string line;
				headers = new Hashtable ();
				while ((line = ReadLine ()) != null && line.Length > 0) {
					int colon = line.IndexOf (':');
					if (colon == -1 || line.Length < colon + 2)
						throw new Exception ();
					string key = line.Substring (0, colon);
					string value = line.Substring (colon + 1).Trim ();
					headers [key] = value;
				}
			} catch (Exception e) {
				throw new Exception ("Error reading headers.");
			}
		}

		public override void CloseConnection ()
		{
			WebTrace.WriteLine ("CloseConnection()");
			stream.Close ();
		}

		public override void FlushResponse (bool finalFlush)
		{
			try {
				if (!headersSent) {
					responseHeaders.Insert (0, serverHeader);
					responseHeaders.Insert (0, status);
					if (!sentConnection)
						responseHeaders.Append ("Connection: close\r\n");

					responseHeaders.Append ("\r\n");
					WriteString (responseHeaders.ToString ());
					headersSent = true;
				}

				byte [] bytes = response.GetBuffer ();
				stream.Write (bytes, 0, (int) response.Length);

				stream.Flush ();
				response.SetLength (0);
				if (finalFlush)
					CloseConnection ();
			} catch (Exception e) {
				WebTrace.WriteLine (e.ToString ());
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
			return true; //FIXME
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

		void WriteString (string s)
		{
			byte [] b = Encoding.GetBytes (s);
			stream.Write (b, 0, b.Length);

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

			return (length + stream.Read (buffer, offset, size));
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
			if (length <= 0)
				return;

			if (data.Length < length)
				length = data.Length;

			response.Write (data, 0, length);
		}
		
		public override void SendStatus (int statusCode, string statusDescription)
		{
			status = String.Format ("HTTP/1.0 {0} {1}\r\n", statusCode, statusDescription);
			WebTrace.WriteLine ("SendStatus() -> " + status);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			WebTrace.WriteLine ("SendUnknownResponseHeader (" + name + ", " + value + ")");
			if (String.Compare (name, "connection", true) == 0)
				sentConnection = true;

			if (!headersSent)
				responseHeaders.AppendFormat ("{0}: {1}\r\n", name, value);
		}
	}
}
