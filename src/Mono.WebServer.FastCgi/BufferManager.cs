//
// ServerManager.cs
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (C) 2007 Brian Nickel
// Copyright (C) 2013 Leonardo Taglialegne
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

using Mono.FastCgi;

namespace Mono.WebServer.FastCgi
{
	class BufferManager
	{
		readonly object buffer_lock = new object ();
		byte [][] buffers = new byte [200][];
		int buffer_count;

		public void AllocateBuffers (out byte[] buffer1, out byte[] buffer2)
		{
			buffer1 = null;
			buffer2 = null;

			lock (buffer_lock) {
				// If there aren't enough existing buffers,
				// create new ones.
				if (buffer_count < 2)
					buffer1 = new byte [Record.SuggestedBufferSize];

				if (buffer_count < 1)
					buffer2 = new byte [Record.SuggestedBufferSize];

				// Now that buffer1 and buffer2 may have been
				// assigned to compensate for a lack of buffers
				// in the array, loop through and assign the
				// remaining values.
				int length = buffers.Length;
				for (int i = 0; i < length && buffer2 == null; i++) {
					if (buffers [i] != null) {
						if (buffer1 == null)
							buffer1 = buffers [i];
						else
							buffer2 = buffers [i];

						buffers [i] = null;
						buffer_count --;
					}
				}
			}
		}

		public void ReleaseBuffers (byte[] buffer1, byte[] buffer2)
		{
			lock (buffer_lock) {
				int length = buffers.Length;
				foreach (byte [] buffer in new [] { buffer1, buffer2 }) {
					if (buffer == null || buffer.Length < Record.SuggestedBufferSize)
						continue;

					// If the buffer count is equal to the
					// length of the buffer array, it needs
					// to be enlarged.
					if (buffer_count == length) {
						EnlargePool ();
						buffers [buffer_count++] = buffer;

						if (buffer == buffer1)
							buffers [buffer_count++] = buffer2;

						return;
					}

					for (int i = 0; i < length; i++) {
						if (buffers [i] == null) {
							buffers [i] = buffer;
							buffer_count ++;
							break;
						}
					}
				}

			}
		}

		void EnlargePool ()
		{
			int length = buffers.Length;
			var buffers_new = new byte[length + length / 3][];
			buffers.CopyTo (buffers_new, 0);
			buffers = buffers_new;
		}
	}
}
