//
// Mono.ASPNET.InitialWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
//
using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace Mono.ASPNET
{
	[Serializable]
	public class RequestData
	{
		public string Verb;
		public string Path;
		public string PathInfo;
		public string QueryString;
		public string Protocol;
		public Hashtable Headers;
		public byte [] InputBuffer;
		public int InputLength;
		public int Position;

		public RequestData (string verb, string path, string queryString, string protocol,
				    Hashtable headers)
		{
			this.Verb = verb;
			this.Path = path;
			this.QueryString = queryString;
			this.Protocol = protocol;
			this.Headers = headers;
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("Verb: {0}\n", Verb);
			sb.AppendFormat ("Path: {0}\n", Path);
			sb.AppendFormat ("PathInfo: {0}\n", PathInfo);
			sb.AppendFormat ("QueryString: {0}\n", QueryString);
			return sb.ToString ();
		}
	}

	public class InitialWorkerRequest
	{
		string verb;
		string path;
		string queryString;
		string protocol;
		string pathInfo;
		Hashtable headers;
		NetworkStream stream;

		byte [] inputBuffer;
		int inputLength;
		int position;
		const int BSize = 1024 * 32;
		
		public InitialWorkerRequest (NetworkStream ns)
		{
			if (ns == null)
				throw new ArgumentNullException ("ns");

			stream = ns;
		}

		void FillBuffer ()
		{
			inputBuffer = new byte [BSize];
			inputLength = stream.Read (inputBuffer, 0, BSize);
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

		bool GetRequestLine ()
		{
			string req = ReadLine ();
			if (req == null)
				return false;

			req = req.Trim ();
			string [] s = req.Split (' ');

			switch (s.Length) {
			case 2:
				verb = s [0].Trim ();
				path = s [1].Trim ();
				break;
			case 3:
				verb = s [0].Trim ();
				path = s [1].Trim ();
				protocol = s [2].Trim ();
				break;
			default:
				return false;
			}

			int qmark = path.IndexOf ('?');
			if (qmark != -1) {
				queryString = path.Substring (qmark + 1);
				path = path.Substring (0, qmark);
			}

			// Decode path
			path = HttpUtility.UrlDecode (path);
			
			// Yes, MS only looks for the '.'. Try setting a handler for
			// something not containing a '.' and you won't get path_info.
			int dot = path.LastIndexOf ('.');
			int slash = (dot != -1) ? path.IndexOf ('/', dot) : 0;
			if (dot > 0 && slash > 0) {
				Console.WriteLine ("path: {0} {1} {2}", path, dot, slash);
				pathInfo = path.Substring (slash);
				path = path.Substring (0, slash);
			}

			if (path.StartsWith ("/~/")) {
				// Not sure about this. It makes request such us /~/dir/file work
				path = path.Substring (2);
			}

			return true;
		}

		bool GetRequestHeaders ()
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

		public void ReadRequestData ()
		{
			if (!GetRequestLine ())
				throw new Exception ("Error reading request line");

			if (protocol == null) {
				protocol = "HTTP/1.0";
			} else 	if (!GetRequestHeaders ()) {
				throw new Exception ("Error getting headers");
			}
		}

		public RequestData RequestData {
			get {
				RequestData rd = new RequestData (verb, path, queryString, protocol, headers);
				rd.InputBuffer = inputBuffer;
				rd.InputLength = inputLength;
				rd.Position = position;
				rd.PathInfo = pathInfo;
				return rd;
			}
		}
	}
}

