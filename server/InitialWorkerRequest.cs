//
// Mono.ASPNET.InitialWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004 Novell, Inc
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace Mono.ASPNET
{
	public class RequestData
	{
		public string Verb;
		public string Path;
		public string PathInfo;
		public string QueryString;
		public string Protocol;
		public byte [] InputBuffer;

		public RequestData (string verb, string path, string queryString, string protocol)
		{
			this.Verb = verb;
			this.Path = path;
			this.QueryString = queryString;
			this.Protocol = protocol;
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

	class RequestLineException : ApplicationException {
		public RequestLineException () : base ("Error reading request line")
		{
		}
	}

	public class InitialWorkerRequest
	{
		string verb;
		string path;
		string queryString;
		string protocol;
		string pathInfo;
		NetworkStream stream;
		bool gotSomeInput;

		byte [] inputBuffer;
		int inputLength;
		int position;
		const int BSize = 1024 * 32;
		
		static Stack bufferStack = new Stack ();
		
		static byte [] AllocateBuffer ()
		{
			lock (bufferStack) {
				if (bufferStack.Count != 0)
					return (byte []) bufferStack.Pop ();
			}
			return new byte [BSize];
		}
		
		static void FreeBuffer (byte [] buf)
		{
			lock (bufferStack) {
				bufferStack.Push (buf);
			}
		}
		
		public InitialWorkerRequest (NetworkStream ns)
		{
			if (ns == null)
				throw new ArgumentNullException ("ns");

			stream = ns;
		}

		void FillBuffer ()
		{
			inputBuffer = AllocateBuffer ();
			inputLength = stream.Read (inputBuffer, 0, BSize);
			if (inputLength == 0) // Socket closed
				throw new IOException ("socket closed");

			gotSomeInput = true;
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
				if (text.Length > 8192)
					throw new InvalidOperationException ("Input line too long.");
			}

			return text.ToString ();
		}

		bool GetRequestLine ()
		{
			string req = null;
			try {
				req = ReadLine ();
			} catch {
			}

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

			path = HttpUtility.UrlDecode (path);
			path = GetSafePath (path);
			
			// Yes, MS only looks for the '.'. Try setting a handler for
			// something not containing a '.' and you won't get path_info.
			int dot = path.LastIndexOf ('.');
			int slash = (dot != -1) ? path.IndexOf ('/', dot) : -1;
			if (dot >= 0 && slash >= 0) {
				pathInfo = path.Substring (slash);
				path = path.Substring (0, slash);
			} else {
				pathInfo = "";
			}

			if (path.StartsWith ("/~/")) {
				// Not sure about this. It makes request such us /~/dir/file work
				path = path.Substring (2);
			}

			return true;
		}

		string GetSafePath (string path)
		{
			string trail = "";
			if (path.EndsWith ("/"))
				trail = "/";

			path = HttpUtility.UrlDecode (path);
			path = path.Replace ('\\','/');
			while (path.IndexOf ("//") != -1)
				path = path.Replace ("//", "/");

			string [] parts = path.Split ('/');
			ArrayList result = new ArrayList (parts.Length);
			
			int end = parts.Length;
			for (int i = 0; i < end; i++) {
				string current = parts [i];
				if (current == "" || current == "." )
					continue;

				if (current == "..") {
					if (result.Count > 0)
						result.RemoveAt (result.Count - 1);
					continue;
				}

				result.Add (current);
			}

			if (result.Count == 0)
				return "/";

			result.Insert (0, "");
			return String.Join ("/", (string []) result.ToArray (typeof (string))) + trail;
		}
		
		public void ReadRequestData ()
		{
			if (!GetRequestLine ())
				throw new RequestLineException ();

			if (protocol == null) {
				protocol = "HTTP/1.0";
			}
		}

		public bool GotSomeInput {
			get { return gotSomeInput; }
		}

		public RequestData RequestData {
			get {
				RequestData rd = new RequestData (verb, path, queryString, protocol);
				byte [] buffer = new byte [inputLength - position];
				Buffer.BlockCopy (inputBuffer, position, buffer, 0, inputLength - position);
				rd.InputBuffer = buffer;
				rd.PathInfo = pathInfo;
				FreeBuffer (inputBuffer);
				return rd;
			}
		}
	}
}

