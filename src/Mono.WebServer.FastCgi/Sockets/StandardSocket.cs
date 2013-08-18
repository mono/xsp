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
using Mono.FastCgi;

namespace Mono.WebServer.FastCgi.Sockets {
	class StandardSocket : Socket {
		readonly System.Net.Sockets.Socket socket;
		readonly System.Net.EndPoint localEndPoint;

		public override IntPtr Handle {
			get {
				return socket.Handle;
			}
		}
		
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
			
			socket = new System.Net.Sockets.Socket (addressFamily, socketType, protocolType);
			this.localEndPoint = localEndPoint;
		}

		public override void Connect ()
		{
			socket.Connect (localEndPoint);
		}
		
		public override void Close ()
		{
			// Make sure that all remaining data are sent and received 
			// before closing; For example, this ensures that all queued 
			// FCGI_STDOUT stream data are sent to the web server for 
			// forwarding to the web client and also that any lingering 
			// FCGI_END_REQUEST response is indeed sent to the web server
			try {
				socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
			} catch (System.Net.Sockets.SocketException) {
				// Ignore
			}
			
			// Only now close the socket
			socket.Close ();
		}
		
		public override int Receive (byte [] buffer, int offset, int size, System.Net.Sockets.SocketFlags flags)
		{
                        // According to the MSDN specification, a call to Sockets.Socket.Receive
                        // (http://msdn.microsoft.com/en-us/library/8s4y8aff.aspx) will return 
                        // 0 immediately, if the remote end has read all incoming data and 
                        // gracefully closed the connection.
                        // 
                        // As all calls to this function are synchronous in the current FastCGI
                        // implementation, we can safely assume that a Receive of 0 bytes would 
                        // only arise in this case.
                        // All other errors are expected to throw an exception.

                        int received = socket.Receive(buffer, offset, size, flags);
                        
                        if (received == 0)
                        {
                            // Note: a better error message would probably be deemed fit.
                            socket.Close();
                            throw new System.Net.Sockets.SocketException();
                        }

			return received;
		}
		
		public override int Send (byte [] data, int offset, int size, System.Net.Sockets.SocketFlags flags)
		{
			return socket.Send (data, offset, size, flags);
		}
		
		public override void Listen (int backlog)
		{
			socket.Bind (localEndPoint);
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
