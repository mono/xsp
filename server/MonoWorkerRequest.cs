//
// Mono.ASPNET.MonoWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	public class MonoWorkerRequest : SimpleWorkerRequest
	{
		TcpClient client;
		MonoApplicationHost appHost;
		TextReader input;
		Stream output;
		string verb;
		string path;
		string queryString;
		string protocol;
		Hashtable headers;
		string [][] unknownHeaders;
		bool headersSent;
		StringBuilder responseHeaders;
		string status;
		ArrayList response;

		public MonoWorkerRequest (TcpClient client, MonoApplicationHost appHost)
			: base (String.Empty, String.Empty, null)
		{
			if (client == null)
				throw new ArgumentNullException ("client");

			if (appHost == null)
				throw new ArgumentNullException ("appHost");

			this.client = client;
			this.appHost = appHost;
			output = client.GetStream ();
			input = new StreamReader (output);
			responseHeaders = new StringBuilder ();
			response = new ArrayList ();
		}

		public override void CloseConnection ()
		{
			Trace.WriteLine ("CloseConnection()");
			input.Close ();
			client.Close ();
		}

		public override void FlushResponse (bool finalFlush)
		{
			try {
			Trace.WriteLine ("FlushResponse()");
			if (!headersSent) {
				StreamWriter w = new StreamWriter (output);
				w.Write (responseHeaders.ToString ());
				w.Write ("\r\n");
				headersSent = true;
			}

			foreach (byte [] bytes in response)
				output.Write (bytes, 0, bytes.Length);

			output.Flush ();
			response.Clear ();
			if (finalFlush)
				CloseConnection ();
				
			} catch (Exception e) {
				Trace.WriteLine (e.ToString ());
			}
		}

		public override string GetAppPath ()
		{
			Trace.WriteLine ("GetAppPath()");
			return appHost.VPath;
		}

		public override string GetAppPathTranslated ()
		{
			Trace.WriteLine ("GetAppPath()");
			return appHost.Path;
		}


		public override string GetFilePath ()
		{
			Trace.WriteLine ("GetFilePath()");
			return path;
		}

		public override string GetFilePathTranslated ()
		{
			Trace.WriteLine ("GetFilePathTranslated()");
			//FIXME: bear in mind virtual directory
			return Path.Combine (appHost.Path, path.Substring (1).Replace ('/', Path.DirectorySeparatorChar));
		}

		public override string GetHttpVerbName ()
		{
			Trace.WriteLine ("GetHttpVerbName()");
			return verb;
		}

		public override string GetHttpVersion ()
		{
			Trace.WriteLine ("GetHttpVersion()");
			return protocol;
		}

		public override string GetKnownRequestHeader (int index)
		{
			string headerName = HttpWorkerRequest.GetKnownRequestHeaderName (index);
			Trace.WriteLine (String.Format ("GetKnownRequestHeader({0}) -> {1}", index, headerName));
			return headers [headerName] as string;
		}

		public override string GetLocalAddress ()
		{
			Trace.WriteLine ("GetLocalAddress()");
			return "localhost";
		}

		public override int GetLocalPort ()
		{
			Trace.WriteLine ("GetLocalPort()");
			return 8080;
		}

		public override string GetPathInfo ()
		{
			Trace.WriteLine ("GetPathInfo()");
			return "GetPathInfo";
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			Trace.WriteLine ("GetPreloadedEntityBody");
			return null;
		}

		public override string GetQueryString ()
		{
			Trace.WriteLine ("GetQueryString()");
			return queryString;
		}

		public override byte [] GetQueryStringRawBytes ()
		{
			Trace.WriteLine ("GetQueryStringRawBytes()");
			if (queryString == null)
				return null;
			return Encoding.Default.GetBytes (queryString);
		}

		public override string GetRawUrl ()
		{
			Trace.WriteLine ("GetRawUrl()");
			if (queryString != null && queryString.Length > 0)
				return path + "/" + queryString;

			return path;
		}

		public override string GetRemoteAddress ()
		{
			Trace.WriteLine ("GetRemoteAddress()");
			return "remoteAddress";
		}

		public override int GetRemotePort ()
		{
			Trace.WriteLine ("GetRemotePort()");
			return 0;
		}


		public override string GetServerName ()
		{
			Trace.WriteLine ("GetServerName()");
			return "localhost";
		}

		public override string GetServerVariable (string name)
		{
			Trace.WriteLine ("GetServerVariable()");
			return "me piden " + name;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			Trace.WriteLine ("GetUnknownRequestHeader()");
			return headers [name] as string;
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			Trace.WriteLine ("GetKnownRequestHeaders()");
			if (unknownHeaders == null) {
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

		public override string GetUriPath ()
		{
			Trace.WriteLine ("GetUriPath()");
			return path;
		}

		public override bool HeadersSent ()
		{
			Trace.WriteLine ("HeadersSent()");
			return headersSent;
		}

		public override bool IsClientConnected ()
		{
			Trace.WriteLine ("IsClientConnected()");
			return true; //FIXME
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return false; //TODO: handle preloading data
		}

		public override string MapPath (string path)
		{
			if (path == null || path.Length == 0 || path == "/")
				return appHost.Path.Replace ('/', Path.DirectorySeparatorChar);

			return Path.Combine (appHost.Path, path.Replace ('/', Path.DirectorySeparatorChar));
		}

		public void ProcessRequest ()
		{
			if (!GetRequestData ())
				return;

			Trace.WriteLine ("ProcessRequest()");
			HttpRuntime.ProcessRequest (this);
		}

		private bool GetRequestLine ()
		{
			string req = input.ReadLine ();
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
				protocol = "HTTP/1.0";
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

			return true;
		}

		private bool GetRequestHeaders ()
		{
			string line;
			headers = new Hashtable ();
			
			while ((line = input.ReadLine ()) != null && line.Length > 0) {
				int colon = line.IndexOf (':');
				if (colon == -1 || line.Length < colon + 2)
					return false;
				
				string key = line.Substring (0, colon);
				string value = line.Substring (colon + 1).Trim ();
				headers [key] = value;
			}

			return true;	
		}

		private bool GetRequestData ()
		{
			if (!GetRequestLine () || !GetRequestHeaders ())
				return false;


			Trace.WriteLine ("verb: {0}", verb);
			Trace.WriteLine ("path: {0}", path);
			Trace.WriteLine ("queryString: {0}", queryString);
			Trace.WriteLine ("protocol: {0}", protocol);
			foreach (string key in headers.Keys)
				Trace.WriteLine (String.Format ("{0}: {1}", key, headers [key]));

			return true;
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			Trace.WriteLine ("ReadEntityBody()");
			if (buffer == null || size == 0)
				return 0;

			char [] chars = new char [size];
			int read;
			if ((read = input.ReadBlock (chars, 0, size)) == 0)
				return 0;

			byte [] bytes = Encoding.Default.GetBytes (chars, 0, read);
			bytes.CopyTo (buffer, 0);
			return bytes.Length;
		}

		public override void SendCalculatedContentLength (int contentLength)
		{
			SendUnknownResponseHeader ("Content-Length", contentLength.ToString ());
		}

		public override void SendKnownResponseHeader (int index, string value)
		{
			if (headersSent)
				return;

			string headerName = HttpWorkerRequest.GetKnownResponseHeaderName (index);
			SendUnknownResponseHeader (headerName, value);
		}

		private void SendStream (Stream stream, long offset, long length)
		{
			if (offset < 0 || length <= 0)
				return;
			
			long stLength = stream.Length;
			if (offset + length > stLength)
				length = stLength - offset;

			if (offset > 0)
				stream.Seek (offset, SeekOrigin.Begin);

			byte [] fileContent = new byte [8192];
			int count = fileContent.Length;
			while (length > 0 && (count = stream.Read (fileContent, 0, count)) != 0) {
				SendResponseFromMemory (fileContent, count);
				length -= count;
				count = (int) Math.Min (length, fileContent.Length);
			}
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			Trace.WriteLine ("SendResponseFromFile()");
			Stream file = null;
			try {
				file = File.OpenRead (filename);
				SendStream (file, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			Trace.WriteLine ("SendResponseFromFile(2)");
			Stream file = null;
			try {
				file = new FileStream (handle, FileAccess.Read);
				SendStream (file, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			Trace.WriteLine ("SendResponseFromMemory()");
			if (length <= 0)
				return;

			byte [] bytes = new byte [length];
			Array.Copy (data, 0, bytes, 0, length);
			response.Add (bytes);
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			Trace.WriteLine ("SendStatus()");
			status = String.Format ("{0} {1} {2}", protocol, statusCode, statusDescription);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			Trace.WriteLine ("SendUnknownResponseHeader()");
			if (!headersSent)
				responseHeaders.AppendFormat ("{0}: {1}\r\n", name, value);
		}
	}

}
