//
// Mono.ASPNET.XSPWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Simon Waite (simon@psionics.demon.co.uk)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
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
		TcpClient client;
		IApplicationHost appHost;
		Stream stream;
		string verb;
		string path;
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

		public XSPWorkerRequest (TcpClient client, IApplicationHost appHost)
			: base (appHost)
		{
			if (client == null)
				throw new ArgumentNullException ("client");

			this.client = client;
			this.appHost = appHost;
			stream = client.GetStream ();
			responseHeaders = new StringBuilder ();
			response = new MemoryStream ();
			status = "HTTP/1.0 200 OK\r\n";
		}

		void FillBuffer ()
		{
			inputBuffer = new byte [2048];
			inputLength = stream.Read (inputBuffer, 0, 2048);
			position = 0;
		}

		int ReadInputByte ()
		{
			if (inputBuffer == null)
				FillBuffer ();

			if (position >= inputLength)
				return stream.ReadByte ();

			return (int) inputBuffer [position++];
		}

		int ReadInput (byte [] buffer, int offset, int size)
		{
			if (inputBuffer == null)
				FillBuffer ();

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

		public override void CloseConnection ()
		{
			WebTrace.WriteLine ("CloseConnection()");
			stream.Close ();
			client.Close ();
		}

		public override void FlushResponse (bool finalFlush)
		{
			try {
				if (!headersSent) {
					responseHeaders.Insert (0, serverHeader);
					responseHeaders.Insert (0, status);
					responseHeaders.Append ("\r\n");
					WriteString (responseHeaders.ToString ());
					headersSent = true;
				}

				byte [] bytes = response.GetBuffer ();
				stream.Write (bytes, 0, bytes.Length);

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
					if (index == -1)
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
			//FIXME
			return "localhost";
		}

		public override int GetLocalPort ()
		{
			WebTrace.WriteLine ("GetLocalPort()");
			//FIXME
			return 8080;
		}

		public override string GetPathInfo ()
		{
			WebTrace.WriteLine ("GetPathInfo()");
			return "GetPathInfo";
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
			if (queryString != null && queryString.Length > 0)
				return path + "?" + queryString;

			return path;
		}

		public override string GetRemoteAddress ()
		{
			WebTrace.WriteLine ("GetRemoteAddress()");
			return "remoteAddress";
		}

		public override int GetRemotePort ()
		{
			WebTrace.WriteLine ("GetRemotePort()");
			return 0;
		}


		public override string GetServerName ()
		{
			WebTrace.WriteLine ("GetServerName()");
			return "localhost";
		}

		public override string GetServerVariable (string name)
		{
			WebTrace.WriteLine ("GetServerVariable()");
			return "GetServerVariable -> " + name;
		}

		public override string GetUriPath ()
		{
			WebTrace.WriteLine ("GetUriPath()");
			return path;
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
				return true;

			if (queryString != null && queryString != "")
				path += "?" + queryString;

			SendStatus (302, "Moved");
			WriteString (status);
			WriteString ("Location: " + path + "\r\n");
			WriteString ("Content-Type: text/html\r\n");
			string page = "<html><head><title>Object moved</title></head>" + 
				      "<body><h1>Object Moved</h1>Document moved <a href=\"" +
				      path + "\">here</a></body>";

			WriteString ("Content-Length: " + page.Length + "\r\n\r\n");
			WriteString (page);
			headersSent = true;
			FlushResponse (true);
			return false;
		}

		void WriteString (string s)
		{
			byte [] b = Encoding.GetBytes (s);
			stream.Write (b, 0, b.Length);
		}

		private bool GetRequestLine ()
		{
			string req = ReadLine ();
			if (req == null)
				return false;

			req = req.Trim ();
			int length = req.Length;
			if (length >= 5 && 0 == String.Compare ("GET ", req.Substring (0, 4), true))
				verb = "GET";
			else if (length >= 6 && 0 == String.Compare ("POST ", req.Substring (0, 5), true))
				verb = "POST";
			else
				throw new InvalidOperationException ("Unsupported method in query: " + req);

			req = req.Substring (verb.Length + 1).TrimStart ();
			string [] s = req.Split (' ');
			length = s.Length;

			switch (length) {
			case 1:
				path = s [0];
				break;
			case 2:
				path = s [0];
				protocol = s [1];
				break;
			default:
				return false;
			}

			int qmark = path.IndexOf ('?');
			if (qmark != -1) {
				queryString = path.Substring (qmark + 1);
				path = path.Substring (0, qmark);
			}

			if (path.StartsWith ("/~/")) {
				// Not sure about this. It makes request such us /~/dir/file work
				path = path.Substring (2);
			}

			return true;
		}

		private bool GetRequestHeaders ()
		{
			string line;
			headers = new Hashtable ();
			
			while ((line = ReadLine ()) != null && line.Length > 0) {
				int colon = line.IndexOf (':');
				if (colon == -1 || line.Length < colon + 2)
					return false;
				
				string key = line.Substring (0, colon);
				string value = line.Substring (colon + 1).Trim ();
				headers [key] = value;
			}

			return true;	
		}

		protected override bool GetRequestData ()
		{
			try {
				if (!GetRequestLine ())
					return false;

				if (protocol == null) {
					protocol = "HTTP/1.0";
				} else 	if (!GetRequestHeaders ()) {
					return false;
				}

				WebTrace.WriteLine ("verb: " + verb);
				WebTrace.WriteLine ("path: " + path);
				WebTrace.WriteLine ("queryString: " + queryString);
				WebTrace.WriteLine ("protocol: " + protocol);
				if (headers != null) {
					foreach (string key in headers.Keys)
						WebTrace.WriteLine (key + ": " + headers [key]);
				}
			} catch (Exception) {
				return false;
			}
			
			return TryDirectory ();
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			WebTrace.WriteLine ("ReadEntityBody()");
			if (buffer == null || size == 0)
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
			status = String.Format ("{0} {1} {2}\r\n", protocol, statusCode, statusDescription);
			WebTrace.WriteLine ("SendStatus() -> " + status);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			WebTrace.WriteLine ("SendUnknownResponseHeader (" + name + ", " + value + ")");
			if (!headersSent)
				responseHeaders.AppendFormat ("{0}: {1}\r\n", name, value);
		}
	}

}

