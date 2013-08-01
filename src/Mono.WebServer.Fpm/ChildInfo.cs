//
// ChildInfo.cs:
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
using System.Diagnostics;
using Mono.FastCgi;
using Mono.WebServer.FastCgi;
using Mono.WebServer.Log;
using System.IO;
using System.Threading;

namespace Mono.WebServer.Fpm {
	struct ChildInfo : IServerCallback<Connection>
	{
		public FastCgi.ConfigurationManager ConfigurationManager { get; set; }

		public Process Process { get; private set; }

		public Func<bool, Process> Spawner { get; set; }

		public string Name { get; set; }

		public bool OnDemand { get; set; }

		public ChildInfo (Process process) : this()
		{
			Process = process;
		}

		public bool TrySpawn ()
		{
			Process = Spawner (OnDemand);
			return Process != null && !Process.HasExited;
		}

		public Connection OnAccept (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			if (Process == null || Process.HasExited) {
				if (TrySpawn ()) {
					Logger.Write (LogLevel.Notice, "Started fastcgi daemon [dynamic] with pid {0} and config file {1}", Process.Id, Path.GetFileName (Name));
					// Let the daemon start
					Thread.Sleep (300);
				}
				else
					throw new Exception ("Couldn't spawn child!");
			}

			Socket onDemandSocket;
			FastCgi.Server.TryCreateUnixSocket (ConfigurationManager.OnDemandSock, out onDemandSocket);
			onDemandSocket.Connect ();
			SocketPassing.SendTo (onDemandSocket.Handle, socket.Handle);
			Logger.Write (LogLevel.Debug, "Sent fd {0} via fd {1}", socket.Handle, onDemandSocket.Handle);
			onDemandSocket.Close ();

			return new Connection (socket);
		}
	}
}
