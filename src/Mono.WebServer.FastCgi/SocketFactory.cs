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

namespace Mono.FastCgi {
	/// <summary>
	///    This static class creates bound instances of <see
	///    cref="Socket" /> to use for various implementations.
	/// </summary>
	public static class SocketFactory
	{
		/// <summary>
		///    Creates a bound socket for a specified local IP end
		///    point.
		/// </summary>
		/// <param name="localEndPoint">
		///    A <see cref="System.Net.IPEndPoint" /> containing the
		///    local IP end point to bind to.
		/// </param>
		/// <returns>
		///    A <see cref="Socket" /> object bound to the specified
		///    end point.
		/// </returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///    An error occurred while binding the socket.
		/// </exception>
		public static Socket CreateTcpSocket (System.Net.IPEndPoint
		                                      localEndPoint)
		{
			return new TcpSocket (localEndPoint);
		}
		
		/// <summary>
		///    Creates a bound socket for a specified IP address and
		///    port.
		/// </summary>
		/// <param name="address">
		///    A <see cref="System.Net.IPAddress" /> containing the
		///    IP address be assigned.
		/// </param>
		/// <param name="port">
		///    A <see cref="int" /> containing the port to bind to.
		/// </param>
		/// <returns>
		///    A <see cref="Socket" /> object bound to the specified
		///    IP address and end point.
		/// </returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///    An error occurred while binding the socket.
		/// </exception>
		public static Socket CreateTcpSocket (System.Net.IPAddress
		                                      address, int port)
		{
			return new TcpSocket (address, port);
		}
		
		/// <summary>
		///    Creates a unix socket for a specified path.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> containing the path to use.
		/// </param>
		/// <returns>
		///    A <see cref="Socket" /> object bound to the specified
		///    path.
		/// </returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///    An error occurred while binding the socket.
		/// </exception>
		public static Socket CreateUnixSocket (string path)
		{
			return new UnixSocket (path);
		}
		
		/// <summary>
		///    Creates a socket from a bound unmanaged socket.
		/// </summary>
		/// <param name="sock">
		///    A <see cref="IntPtr" /> pointing to the bound socket.
		/// </param>
		/// <returns>
		///    A <see cref="Socket" /> object bound to the specified
		///    IP address and end point.
		/// </returns>
		/// <exception cref="System.Net.Sockets.SocketException">
		///    The specified socket is not bound.
		/// </exception>
		public static Socket CreatePipeSocket (IntPtr sock)
		{
			return new UnmanagedSocket (sock);
		}
	}
}
