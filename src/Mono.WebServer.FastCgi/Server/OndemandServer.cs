using System;
using Mono.FastCgi;

namespace Mono.WebServer.FastCgi {
	public class OndemandServer : IServerCallback<ConnectionProxy>, IServer
	{
		readonly GenericServer<ConnectionProxy> backend;
		readonly Mono.FastCgi.Server server;

		int max_requests = Int32.MaxValue;

		public event EventHandler RequestReceived;

		public int MaxConnections {
			get { return backend.MaxConnections; }
			set { backend.MaxConnections = value; }
		}

		public int MaxRequests {
			get {return max_requests;}
			set {
				if (value < 1)
					throw new ArgumentOutOfRangeException (
						"value",
						Strings.Server_MaxReqsOutOfRange);

				max_requests = value;
			}
		}

		public bool MultiplexConnections { get; set; }

		public OndemandServer (Socket socket)
		{
			backend = new GenericServer<ConnectionProxy> (socket, this);
			server = new Mono.FastCgi.Server (socket);
		}

		public void Start (bool background, int backlog)
		{
			backend.Start (background, backlog);
		}

		public void Stop ()
		{
			backend.Stop ();
		}

		public ConnectionProxy OnAccept (Socket socket)
		{
			return new ConnectionProxy(new Connection (socket, server));
		}
	}
}

