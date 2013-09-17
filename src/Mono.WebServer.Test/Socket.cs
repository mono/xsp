//
// Socket.cs
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using Mono.Unix;
using Mono.WebServer.FastCgi;
using NUnit.Framework;

namespace Mono.WebServer.Test {
	[TestFixture]
	public class SocketTest
	{
		const string FASTCGI_SOCKET_PATH = "\0fastcgi";
		const string FPM_SOCKET_PATH = "\0fpm";
		const string MESSAGE = "Foo!";

		[Test]
		public void TestPassUnixFd ()
		{
			using (ListenUnix (FASTCGI_SOCKET_PATH, FastCgiAccept))
			using (ListenUnix (FPM_SOCKET_PATH, FpmUnixAccept)) {
				var webserver = new UnixClient (FPM_SOCKET_PATH);
				string read;
				using (var stream = webserver.GetStream ())
				using (var reader = new StreamReader(stream))
					read = reader.ReadLine ();
				Assert.AreEqual (MESSAGE, read);
			}
		}

		[Test]
		public void TestPassTcpFd ()
		{
			using (ListenUnix (FASTCGI_SOCKET_PATH, FastCgiAccept)) {
				var portAndSocket = ListenTcp (FpmTcpAccept);

				using (portAndSocket.Item2) {
					var webserver = new TcpClient ();
					webserver.Connect (IPAddress.Loopback, portAndSocket.Item1);
					string read;
					using (var stream = webserver.GetStream ())
					using (var reader = new StreamReader(stream))
						read = reader.ReadLine ();
					Assert.AreEqual (MESSAGE, read);
				}
			}
		}

		static IDisposable ListenUnix (string path, AsyncCallback accept)
		{
			var socket = new UnixClient ();
			var client = socket.Client;
			client.Bind (new UnixEndPoint (path));
			client.Listen (1);
			client.BeginAccept (accept, client);
			return socket;
		}

		static Tuple<int,IDisposable> ListenTcp (AsyncCallback accept)
		{
			var socket = new TcpListener (IPAddress.Loopback, 0);
			socket.Start ();
			socket.BeginAcceptSocket (accept, socket);
			var endpoint = socket.LocalEndpoint as IPEndPoint;
			if (endpoint == null)
				Assert.Fail ("LocalEndPoint wasn't an IPEndPoint");
			return new Tuple<int,IDisposable>(endpoint.Port, socket.Server);
		}

		static void FastCgiAccept(IAsyncResult res)
		{
			if (!res.IsCompleted)
				throw new ArgumentException("res");

			var socket = res.AsyncState as Socket;
			if (socket == null)
				throw new ArgumentNullException ("state");

			using (var connection = socket.EndAccept (res))
			{
				using (var stream = SocketPassing.ReceiveFrom (connection))
				using (var writer = new StreamWriter(stream))
					writer.WriteLine (MESSAGE);
			}
		}

		static void FpmUnixAccept(IAsyncResult res)
		{
			if (!res.IsCompleted)
				throw new ArgumentException("res");

			var socket = res.AsyncState as Socket;
			if (socket == null)
				throw new ArgumentNullException ("state");

			using (var connection = socket.EndAccept (res)) {
				var back = new UnixClient (FASTCGI_SOCKET_PATH);
				SocketPassing.SendTo (back.Client, connection.Handle);
			}
		}

		static void FpmTcpAccept(IAsyncResult res)
		{
			if (!res.IsCompleted)
				throw new ArgumentException("res");

			var socket = res.AsyncState as TcpListener;
			if (socket == null)
				throw new ArgumentNullException ("state");

			using (var connection = socket.EndAcceptSocket (res)) {
				var back = new UnixClient (FASTCGI_SOCKET_PATH);
				SocketPassing.SendTo (back.Client, connection.Handle);
			}
		}
	}
}

