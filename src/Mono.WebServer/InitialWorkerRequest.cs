//
// Mono.WebServer.InitialWorkerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2010 Novell, Inc
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace Mono.WebServer
{
	public class InitialWorkerRequest
	{
		string verb;
		string path;
		string queryString;
		string protocol;
		readonly Stream stream;

		int inputLength;
		int position;
		const int B_SIZE = 1024 * 32;
		
		static readonly Stack<byte[]> bufferStack = new Stack<byte[]> ();
		static readonly Encoding encoding = Encoding.GetEncoding (28591);

		public bool GotSomeInput { get; private set; }

		public byte [] InputBuffer { get; private set; }

		public RequestData RequestData {
			get {
				var rd = new RequestData (verb, path, queryString, protocol);
				var buffer = new byte [inputLength - position];
				Buffer.BlockCopy (InputBuffer, position, buffer, 0, inputLength - position);
				rd.InputBuffer = buffer;
				return rd;
			}
		}
		
		public static byte [] AllocateBuffer ()
		{
			lock (bufferStack) {
				if (bufferStack.Count != 0)
					return bufferStack.Pop ();
			}
			return new byte [B_SIZE];
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
			if (InputBuffer != null)
				FreeBuffer (InputBuffer);
		}

		public void SetBuffer (byte [] buffer, int length)
		{
			InputBuffer = buffer;
			inputLength = length;
			GotSomeInput = (length > 0);
			position = 0;
		}

		void FillBuffer ()
		{
			position = 0;
			InputBuffer = AllocateBuffer ();
			inputLength = stream.Read (InputBuffer, 0, B_SIZE);
			if (inputLength == 0) // Socket closed
				throw new IOException ("socket closed");

			GotSomeInput = true;
		}

		string ReadRequestLine ()
		{
			var text = new StringBuilder ();
			do {
				if (InputBuffer == null || position >= inputLength)
					FillBuffer ();

				if (position >= inputLength)
					break;
				
				bool cr = false;
				int count = 0;
				byte b = 0;
				int i;
				for (i = position; count < 8192 && i < inputLength; i++, count++) {
					b = InputBuffer [i];
					if (b == '\r') {
						cr = true;
						count--;
						continue;
					}
					if (b == '\n' || cr) {
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

				text.Append (encoding.GetString (InputBuffer, position, count));
				position = i + 1;

				if (i >= inputLength) {
					b = InputBuffer [inputLength - 1];
					if (b != '\r' && b != '\n')
						continue;
					FillBuffer();
					if (b == '\r' && inputLength > 0 && InputBuffer[0] == '\n')
						position++;
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
			string req;
			try {
				while (true) {
					req = ReadRequestLine ();
					if (req == null) {
						GotSomeInput = false;
						return false;
					}

					req = req.Trim ();
					// Ignore empty lines before the actual request.
					if (req.Length > 0)
						break;
				}
			} catch (Exception) {
				GotSomeInput = false;
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
			
			path = GetSafePath (path);
			if (path.StartsWith ("/~/")) {
				// Not sure about this. It makes request such us /~/dir/file work
				path = path.Substring (2);
			}

			return true;
		}

		static string GetSafePath (string path)
		{
			bool appendSlash = path.EndsWith ("/");

			path = HttpUtility.UrlDecode (path);
			path = path.Replace ('\\','/');
			while (path.IndexOf ("//") != -1)
				path = path.Replace ("//", "/");

			string [] parts = path.Split ('/');
			var result = new List<string> (parts.Length);
			
			int end = parts.Length;
			for (int i = 0; i < end; i++) {
				string current = parts [i];
				if (current.Length == 0 || current == "." )
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

			var res = new StringBuilder();
			foreach (var part in result) {
				res.Append ('/');
				res.Append (part);
			}

			if (appendSlash)
				res.Append ('/');
			return res.ToString ();
		}
		
		public void ReadRequestData ()
		{
			if (!GetRequestLine ())
				throw new RequestLineException ();

			if (protocol == null) {
				protocol = "HTTP/1.0";
			}
		}
	}
}

