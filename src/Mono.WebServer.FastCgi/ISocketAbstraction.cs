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
	/// <summary>
	///    This abstract class provides a wrapper around socket methods and
	///    is to be removed once a FILDES solution has been reached.
	/// </summary>
	public abstract class Socket
	{
		/// <seealso cref="System.Net.Sockets.Socket.Close()" />
		public abstract void Close ();
		
		/// <seealso cref="System.Net.Sockets.Socket.Receive(byte[],int,int,System.Net.Sockets.SocketFlags)" />
		public abstract int Receive (byte [] buffer, int offset, int size, System.Net.Sockets.SocketFlags flags);
		
		/// <seealso cref="System.Net.Sockets.Socket.Send(byte[],int,int,System.Net.Sockets.SocketFlags)" />
		public abstract int Send (byte [] data, int offset, int size, System.Net.Sockets.SocketFlags flags);
		
		/// <seealso cref="System.Net.Sockets.Socket.Listen" />
		public abstract void Listen (int backlog);
		
		/// <seealso cref="System.Net.Sockets.Socket.BeginAccept(AsyncCallback,object)" />
		public abstract IAsyncResult BeginAccept (AsyncCallback callback, object state);
		
		/// <seealso cref="System.Net.Sockets.Socket.EndAccept(IAsyncResult)" />
		public abstract Socket EndAccept (IAsyncResult asyncResult);
		
		/// <seealso cref="System.Net.Sockets.Socket.Connected" />
		public abstract bool Connected {get;}
	}
}