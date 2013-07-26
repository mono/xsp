using System;
using NUnit.Framework;
using Mono.Unix;
using System.IO;
using System.Net.Sockets;
using Mono.WebServer.Fpm;
using System.Net;
using Mono.WebServer.FastCgi;

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

		void FastCgiAccept(IAsyncResult res)
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

		void FpmUnixAccept(IAsyncResult res)
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

		void FpmTcpAccept(IAsyncResult res)
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

