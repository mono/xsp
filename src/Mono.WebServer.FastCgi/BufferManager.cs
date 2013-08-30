//
// BufferManager.cs
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
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

using System;
using System.Collections.Generic;
using Mono.WebServer.FastCgi.Compatibility;

namespace Mono.WebServer.FastCgi
{
	public class BufferManager
	{
		const int CLIENTS_PER_BUFFER = 100;

		readonly int segmentSize;

		/// <summary>
		/// The size of a complete buffer.
		/// Warning: the code assumes this to be an exact multiple of <see cref="segmentSize" />
		/// </summary>
		readonly int bufferSize;
		readonly object bufferLock = new object ();
		readonly Stack<CompatArraySegment<byte>> buffers = new Stack<CompatArraySegment<byte>>();

		public BufferManager (int segmentSize)
		{
			if (segmentSize < 0)
				throw new ArgumentOutOfRangeException ("segmentSize", segmentSize, "Should be positive");

			this.segmentSize = segmentSize;
			bufferSize = CLIENTS_PER_BUFFER * this.segmentSize;
		}

		public void ReturnBuffer(CompatArraySegment<byte> buffer)
		{
			lock (bufferLock)
			{
				buffers.Push(buffer);
				// TODO: if we have enough buffers we could release some
			}
		}

		void Expand ()
		{
			var toadd = new byte[bufferSize];
			for (int i = 0; i < bufferSize; i += segmentSize)
				buffers.Push (new CompatArraySegment<byte> (toadd, i, segmentSize));
		}

		public CompatArraySegment<byte> ClaimBuffer ()
		{
			lock (bufferLock)
			{
				if (buffers.Count < 1)
				{
					Expand();
				}

				return buffers.Pop();
			}
		}
	}
}
