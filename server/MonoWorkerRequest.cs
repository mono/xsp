//
// Mono.ASPNET.MonoWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Simon White (simon@psionics.demon.co.uk)
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
		static Encoding encoding = new UTF8Encoding (false);

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
			status = "HTTP/1.0 200 OK\r\n";
		}

		public override void CloseConnection ()
		{
			WebTrace.WriteLine ("CloseConnection()");
			input.Close ();
			client.Close ();
		}

		public override void FlushResponse (bool finalFlush)
		{
			try {
				WebTrace.WriteLine ("FlushResponse({0}), {1}", finalFlush, headersSent);
				if (!headersSent) {
					responseHeaders.Insert (0, status);
					responseHeaders.Append ("\r\n");
					byte [] b = encoding.GetBytes (responseHeaders.ToString ());
					output.Write (b, 0, b.Length);
					headersSent = true;
				}

				foreach (byte [] bytes in response)
					output.Write (bytes, 0, bytes.Length);

				output.Flush ();
				response.Clear ();
				if (finalFlush)
					CloseConnection ();
			} catch (Exception e) {
				WebTrace.WriteLine (e.ToString ());
			}
		}

		public override string GetAppPath ()
		{
			WebTrace.WriteLine ("GetAppPath()");
			return appHost.VPath;
		}

		public override string GetAppPathTranslated ()
		{
			WebTrace.WriteLine ("GetAppPath()");
			return appHost.Path;
		}


		public override string GetFilePath ()
		{
			WebTrace.WriteLine ("GetFilePath()");
			return path;
		}

		public override string GetFilePathTranslated ()
		{
			WebTrace.WriteLine ("GetFilePathTranslated()");
			//FIXME: bear in mind virtual directory
			return Path.Combine (appHost.Path, path.Substring (1).Replace ('/', Path.DirectorySeparatorChar));
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

		public override string GetLocalAddress ()
		{
			WebTrace.WriteLine ("GetLocalAddress()");
			return "localhost";
		}

		public override int GetLocalPort ()
		{
			WebTrace.WriteLine ("GetLocalPort()");
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
			return encoding.GetBytes (queryString);
		}

		public override string GetRawUrl ()
		{
			WebTrace.WriteLine ("GetRawUrl()");
			if (queryString != null && queryString.Length > 0)
				return path + "/" + queryString;

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
			return "me piden " + name;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			WebTrace.WriteLine ("GetUnknownRequestHeader()");
			if (headers == null)
				return null;

			return headers [name] as string;
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			WebTrace.WriteLine ("GetKnownRequestHeaders()");
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

			WebTrace.WriteLine ("ProcessRequest()");
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
			
			return true;
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			WebTrace.WriteLine ("ReadEntityBody()");
			if (buffer == null || size == 0)
				return 0;

			char [] chars = new char [size];
			int read;
			if ((read = input.ReadBlock (chars, 0, size)) == 0)
				return 0;

			byte [] bytes = encoding.GetBytes (chars, 0, read);
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
			WebTrace.WriteLine ("SendResponseFromFile()");
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
			WebTrace.WriteLine ("SendResponseFromFile(2)");
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
			WebTrace.WriteLine ("SendResponseFromMemory()");
			if (length <= 0)
				return;

			byte [] bytes = new byte [length];
			Array.Copy (data, 0, bytes, 0, length);
			response.Add (bytes);
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
