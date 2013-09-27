//
// OnDemandServer.cs
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
using Mono.WebServer.Log;
using Mono.FastCgi;
using Mono.WebServer.FastCgi.Sockets;

namespace Mono.WebServer.FastCgi {
	public class OnDemandServer : IServerCallback<ConnectionProxy>, IServer
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
					throw new ArgumentOutOfRangeException ("value", Strings.Server_MaxReqsOutOfRange);

				max_requests = value;
			}
		}

		public bool MultiplexConnections { get; set; }

		public OnDemandServer (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			backend = new GenericServer<ConnectionProxy> (socket, this);
			var fakeBackend = new FakeGenericServer<ConnectionProxy> (backend);
			server = new Mono.FastCgi.Server (fakeBackend);
		}

		public bool Start (bool background, int backlog)
		{
			return backend.Start (background, backlog);
		}

		public void Stop ()
		{
			backend.Stop ();
		}

		public ConnectionProxy OnAccept (Socket socket)
		{
			IntPtr fd = SocketPassing.ReceiveFrom (socket.Handle);
			Logger.Write (LogLevel.Debug, "Received fd {0} from {1}", fd, socket.Handle);
			Socket passed = new UnmanagedSocket (fd, true);
			return new ConnectionProxy (new Connection (passed, server));
		}

		public void SetResponder (Type type)
		{
			server.SetResponder (type);
		}
	}
}

