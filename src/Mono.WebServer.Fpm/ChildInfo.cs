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
using Mono.Unix;

namespace Mono.WebServer.Fpm {
	struct ChildInfo : IServerCallback<Connection>
	{
		public string OnDemandSock { get; set; }

		public Process Process { get; private set; }

		public Func<Process> Spawner { get; set; }

		public string Name { get; set; }

		public bool TrySpawn ()
		{
			Process = Spawner ();
			return Process != null && !Process.HasExited;
		}

		public Connection OnAccept (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			Logger.Write (LogLevel.Debug, "ChildInfo.OnAccept");
			if (Process == null || Process.HasExited) {
				if (TrySpawn ()) {
					var process = Process;
					int id = process.Id;
					Logger.Write (LogLevel.Notice, "Started fastcgi daemon [dynamic] with pid {0} and config file {1}", id, Path.GetFileName (Name));
					process.EnableRaisingEvents = true;
					process.Exited += (sender, e) => Logger.Write (LogLevel.Notice, "Fastcgi daemon [dynamic] with pid {0} exited [it is {1}ly dead]", id, process.HasExited.ToString ().ToLowerInvariant ());
					// Let the daemon start
					Thread.Sleep (300);
				} else
					throw new Exception ("Couldn't spawn child!");
			} else
				Logger.Write (LogLevel.Debug, "Recycling child with pid {0}", Process.Id);

			Logger.Write (LogLevel.Debug, "Will connect to backend");
			var onDemandSocket = new UnixClient ();
			Logger.Write (LogLevel.Debug, "Connecting to backend on {0}", OnDemandSock);
			if (OnDemandSock.StartsWith ("\\0", StringComparison.Ordinal))
				onDemandSocket.Connect ('\0' + OnDemandSock.Substring (2));
			else {
				Uri uri;
				if (Uri.TryCreate (OnDemandSock, UriKind.Absolute, out uri))
					onDemandSocket.Connect (uri.PathAndQuery);
				else
					onDemandSocket.Connect (OnDemandSock);
			}
			SocketPassing.SendTo (onDemandSocket.Client.Handle, socket.Handle);
			Logger.Write (LogLevel.Debug, "Sent fd {0} via fd {1}", socket.Handle, onDemandSocket.Client.Handle);
			onDemandSocket.Close ();

			return new Connection (socket);
		}

		public override string ToString ()
		{
			return string.Format ("[ChildInfo: Name={0}]", Name);
		}
	}
}
