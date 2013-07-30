//
// SocketAbstractions/Socket.cs: Abstracts socket operations.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

namespace Mono.FastCgi {
	public abstract class Socket
	{
		public abstract IntPtr Handle { get; }

		public abstract void Connect ();

		public abstract void Close ();
		
		public abstract int Receive (byte [] buffer, int offset, int size, System.Net.Sockets.SocketFlags flags);
		
		public abstract int Send (byte [] data, int offset, int size, System.Net.Sockets.SocketFlags flags);
		
		public abstract void Listen (int backlog);
		
		public abstract IAsyncResult BeginAccept (AsyncCallback callback, object state);
		
		public abstract Socket EndAccept (IAsyncResult asyncResult);
		
		public abstract bool Connected {get;}
	}
}