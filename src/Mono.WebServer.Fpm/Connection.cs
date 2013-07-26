using System;
using Mono.WebServer.FastCgi;
using Mono.FastCgi;
using Mono.WebServer.Log;

namespace Mono.WebServer.Fpm {
	public class Connection : IConnection
	{
		readonly Socket socket;

		public Connection (Socket socket)
		{
			this.socket = socket;
		}

		public event EventHandler RequestReceived;

		public void Run ()
		{
		}

		public void Stop ()
		{
			try {
				if(socket.Connected)
					socket.Close ();
			} catch (Exception e) {
				Logger.Write (e);
			}
		}
	}
}

