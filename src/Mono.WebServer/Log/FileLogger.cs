//
// FileLogger.cs
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (c) 2013 Leonardo Taglialegne.
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
using System.IO;

namespace Mono.WebServer.Log {
	class FileLogger : ILogger {
		StreamWriter writer;

		readonly object write_lock = new object ();

		~FileLogger ()
		{
			Close ();
		}

		public void Open (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			lock (write_lock) {
				Close ();
				Stream stream = File.Open (path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				stream.Seek (0, SeekOrigin.End);
				writer = new StreamWriter (stream);
			}
		}

		public void Write (LogLevel level, string text)
		{
			if (writer == null)
				return;

			lock (write_lock) {
				if (writer == null)
					return;

				writer.WriteLine (text);
				writer.Flush ();
			}
		}

		public void Close ()
		{
			lock (write_lock) {
				if (writer == null)
					return;

				try {
					writer.Flush ();
					writer.Close ();
				} catch (ObjectDisposedException) {
					// Already done
				}
				writer = null;
			}
		}
	}
}
