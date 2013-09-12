//
// Mono.WebServer.ModMonoTCPWebSource
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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
using System.Net;
using System.Net.Sockets;
using Mono.WebServer.Log;

namespace Mono.WebServer
{
	public class ModMonoTCPWebSource: ModMonoWebSource
	{
		IPEndPoint bindAddress;

		public ModMonoTCPWebSource (IPAddress address, int port, string lockfile)
			: base (lockfile)
		{
			if (address == IPAddress.Any)
				address = IPAddress.Loopback;

			SetListenAddress (address, port);
		}
		
		public void SetListenAddress (int port)
		{
			SetListenAddress (IPAddress.Any, port);
		}

		public void SetListenAddress (IPAddress address, int port) 
		{
			SetListenAddress (new IPEndPoint (address, port));
		}

		public void SetListenAddress (IPEndPoint bindAddress)
		{
			if (bindAddress == null)
				throw new ArgumentNullException ("bindAddress");

			this.bindAddress = bindAddress;
		}

		public override bool GracefulShutdown ()
		{
			EndPoint ep = bindAddress;
			var sock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			try {
				sock.Connect (ep);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error, "Cannot connect to {0}: {1}", ep, e.Message);
				return false;
			}

			return SendShutdownCommandAndClose (sock);
		}

		public override Socket CreateSocket ()
		{
			if (bindAddress == null)
				throw new InvalidOperationException ("No address/port to listen");

			var listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (bindAddress);
			return listen_socket;
		}
	}
}

