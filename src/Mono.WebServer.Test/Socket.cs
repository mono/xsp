using System;
using NUnit.Framework;
using Mono.Unix;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Mono.WebServer.Fpm;

namespace Mono.WebServer.Test {
	[TestFixture()]
	public class SocketTest
	{
		const string fastcgiSocketPath = "\0fastcgi";
		const string fpmSocketPath = "\0fpm";
		const string message = "Foo!";

		[Test()]
		public void TestPassFd ()
		{
			Listen (fastcgiSocketPath, FastCgiAccept);
			Listen (fpmSocketPath, FpmAccept);

			UnixClient webserver = new UnixClient (fpmSocketPath);
			string read;
			using (var stream = webserver.GetStream ())
			using (var reader = new StreamReader(stream))
				read = reader.ReadLine ();
			Assert.AreEqual (message, read);
		}

		static void Listen (string path, AsyncCallback accept)
		{
			UnixClient socket = new UnixClient ();
			var client = socket.Client;
			client.Bind (new UnixEndPoint (path));
			client.Listen (1);
			client.BeginAccept (accept, client);
		}

		void FastCgiAccept(IAsyncResult res)
		{
			if (!res.IsCompleted)
				throw new ArgumentException("res");

			var socket = res.AsyncState as System.Net.Sockets.Socket;
			if (socket == null)
				throw new ArgumentNullException ("state");

			using (var connection = socket.EndAccept (res))
			{
				using (var stream = SocketPassing.ReceiveFrom (connection))
				using (var writer = new StreamWriter(stream))
					writer.WriteLine (message);
			}
		}

		void FpmAccept(IAsyncResult res)
		{
			if (!res.IsCompleted)
				throw new ArgumentException("res");

			var socket = res.AsyncState as System.Net.Sockets.Socket;
			if (socket == null)
				throw new ArgumentNullException ("state");

			using (var connection = socket.EndAccept (res)) {
				UnixClient back = new UnixClient (fastcgiSocketPath);
				SocketPassing.SendTo (back.Client, connection.Handle);
			}
		}
	}
}

