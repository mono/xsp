//
// SocketAbstractions/TcpSocket.cs: Provides support for bound TCP sockets.
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
	internal class TcpSocket : StandardSocket
	{
		public TcpSocket (System.Net.IPEndPoint localEndPoint)
			: base (System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream,
			        System.Net.Sockets.ProtocolType.IP, localEndPoint)
		{
		}
		
		public TcpSocket (System.Net.IPAddress address, int port)
			: this (new System.Net.IPEndPoint (address == System.Net.IPAddress.Any ?
				System.Net.IPAddress.Loopback : address, port))
		{
		}
	}
}