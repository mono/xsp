//
// SocketPassing.cs:
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
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Mono.WebServer.FastCgi {
	public static class SocketPassing
	{
		public static void SendTo (IntPtr connection, IntPtr passing)
		{
			send_fd (connection, passing);
		}

		public static void SendTo (Socket connection, IntPtr passing)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			send_fd (connection.Handle, passing);
		}

		public static IntPtr ReceiveFrom (IntPtr connection)
		{
			IntPtr fd;
			recv_fd (connection, out fd);
			return fd;
		}

		public static UnixStream ReceiveFrom (Socket connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			return new UnixStream (ReceiveFrom (connection.Handle).ToInt32 ());
		}

		[DllImport ("fpm_helper")]
		static extern void send_fd (IntPtr sock, IntPtr fd);

		[DllImport ("fpm_helper")]
		static extern void recv_fd (IntPtr sock, out IntPtr fd);
	}
}

