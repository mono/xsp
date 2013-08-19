//
// SocketFactory.cs: Creates bound sockets of various types to use.
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
using Mono.WebServer.FastCgi.Sockets;

namespace Mono.FastCgi {
	[Obsolete]
	public static class SocketFactory
	{
		public static Socket CreateTcpSocket (System.Net.IPEndPoint
		                                      localEndPoint)
		{
			if (localEndPoint == null)
				throw new ArgumentNullException ("localEndPoint");
			return new TcpSocket (localEndPoint);
		}
		
		public static Socket CreateTcpSocket (System.Net.IPAddress
		                                      address, int port)
		{
			if (address == null)
				throw new ArgumentNullException ("address");
			return new TcpSocket (address, port);
		}
		
		public static Socket CreateUnixSocket (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			return new UnixSocket (path);
		}

		public static Socket CreateUnixSocket (string path, ushort? permissions)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			return new UnixSocket (path, permissions);
		}
		
		public static Socket CreatePipeSocket (IntPtr sock)
		{
			return new UnmanagedSocket (sock);
		}
	}
}
