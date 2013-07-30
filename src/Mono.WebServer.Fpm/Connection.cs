using System;
using Mono.WebServer.FastCgi;
using Mono.FastCgi;
using Mono.WebServer.Log;

namespace Mono.WebServer.Fpm {
	public class Connection : IConnection
	{
		readonly Socket socket;

		public event EventHandler RequestReceived;

		public Connection (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			this.socket = socket;
		}

		public void Run ()
		{
		}

		public void Stop ()
		{
			try {
				if (socket.Connected)
					socket.Close ();
			} catch (Exception e) {
				Logger.Write (e);
			}
		}
	}
}

