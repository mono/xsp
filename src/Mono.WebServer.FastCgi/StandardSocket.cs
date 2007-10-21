//
// SocketAbstractions/StandardSocket.cs: Provides a wrapper around a standard
// .NET socket.
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
	internal class StandardSocket : Socket
	{
		private System.Net.Sockets.Socket socket;
		
		public StandardSocket (System.Net.Sockets.Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			
			this.socket = socket;
		}
		
		public StandardSocket (System.Net.Sockets.AddressFamily addressFamily,
		                       System.Net.Sockets.SocketType socketType,
		                       System.Net.Sockets.ProtocolType protocolType,
		                       System.Net.EndPoint localEndPoint)
		{
			if (localEndPoint == null)
				throw new ArgumentNullException ("localEndPoint");
			
			socket = new System.Net.Sockets.Socket (addressFamily, socketType,
				protocolType);
			
			socket.Bind (localEndPoint);
		}
		
		public override void Close ()
		{
			socket.Close ();
		}
		
		public override int Receive (byte [] buffer, int offset, int size, System.Net.Sockets.SocketFlags flags)
		{
			return socket.Receive (buffer, offset, size, flags);
		}
		
		public override int Send (byte [] data, int offset, int size, System.Net.Sockets.SocketFlags flags)
		{
			return socket.Send (data, offset, size, flags);
		}
		
		public override void Listen (int backlog)
		{
			socket.Listen (backlog);
		}
		
		public override IAsyncResult BeginAccept (AsyncCallback callback,
		                                 object state)
		{
			return socket.BeginAccept (callback, state);
		}
		
		public override Socket EndAccept (IAsyncResult asyncResult)
		{
			return new StandardSocket (socket.EndAccept (asyncResult));
		}
		
		public override bool Connected {
			get {return socket.Connected;}
		}

	}
}