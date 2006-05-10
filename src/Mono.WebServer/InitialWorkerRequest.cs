//
// Mono.WebServer.InitialWorkerRequest
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

namespace Mono.WebServer
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
		Stream stream;
		bool gotSomeInput;

		byte [] inputBuffer;
		int inputLength;
		int position;
		const int BSize = 1024 * 32;
		
		static Stack bufferStack = new Stack ();
		static Encoding encoding = Encoding.GetEncoding (28591);
		
		public static byte [] AllocateBuffer ()
		{
			lock (bufferStack) {
				if (bufferStack.Count != 0)
					return (byte []) bufferStack.Pop ();
			}
			return new byte [BSize];
		}
		
		public static void FreeBuffer (byte [] buf)
		{
			if (buf == null)
				return;

			lock (bufferStack) {
				bufferStack.Push (buf);
			}
		}
		
		public InitialWorkerRequest (Stream ns)
		{
			if (ns == null)
				throw new ArgumentNullException ("ns");

			stream = ns;
		}

		public void FreeBuffer ()
		{
			if (inputBuffer != null)
				FreeBuffer (inputBuffer);
		}

		public void SetBuffer (byte [] buffer, int length)
		{
			inputBuffer = buffer;
			inputLength = length;
			gotSomeInput = (length > 0);
			position = 0;
		}

		void FillBuffer ()
		{
			position = 0;
			inputBuffer = AllocateBuffer ();
			inputLength = stream.Read (inputBuffer, 0, BSize);
			if (inputLength == 0) // Socket closed
				throw new IOException ("socket closed");

			gotSomeInput = true;
		}

		string ReadRequestLine ()
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

				text.Append (encoding.GetString (inputBuffer, position, count));
				position = i + 1;

				if (i >= inputLength) {
					b = inputBuffer [inputLength - 1];
					if (b != '\r' && b != '\n')
						continue;
				}
				break;
			} while (true);

			if (text.Length == 0) {
				return null;
			}

			return text.ToString ();
		}

		bool GetRequestLine ()
		{
			string req = null;
			try {
				while (true) {
					req = ReadRequestLine ();
					if (req == null) {
						gotSomeInput = false;
						return false;
					}

					req = req.Trim ();
					// Ignore empty lines before the actual request.
					if (req != "")
						break;
				}
			} catch (Exception) {
				gotSomeInput = false;
				return false;
			}

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

		public byte [] InputBuffer {
			get { return inputBuffer; }
		}

		public RequestData RequestData {
			get {
				RequestData rd = new RequestData (verb, path, queryString, protocol);
				byte [] buffer = new byte [inputLength - position];
				Buffer.BlockCopy (inputBuffer, position, buffer, 0, inputLength - position);
				rd.InputBuffer = buffer;
				return rd;
			}
		}
	}
}

