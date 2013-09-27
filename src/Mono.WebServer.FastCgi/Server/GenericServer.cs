//
// GenericServer.cs
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
using System.Collections.Generic;
using System.Threading;
using Mono.WebServer.Log;
using Mono.FastCgi;

namespace Mono.WebServer.FastCgi
{
	public class GenericServer<T> : IGenericServer<T> where T : IConnection
	{
		readonly Socket listen_socket;
		readonly IServerCallback<T> serverCallback;
		readonly object accept_lock = new object ();
		readonly object state_lock = new object ();
		bool accepting;
		bool stopped;
		Thread runner;
		readonly List<T> connections = new List<T> ();
		readonly object connections_lock = new object ();

		int max_connections = Int32.MaxValue;
		readonly AutoResetEvent stop_signal = new AutoResetEvent (false);
		readonly AutoResetEvent stopped_signal = new AutoResetEvent (false);

		public event EventHandler RequestReceived;

		public IEnumerable<T> Connections {
			get { lock(connections_lock) return new List<T> (connections); }
		} 

		public bool Started { get; private set; }

		public bool CanAccept {
			get { return Started && connections.Count < max_connections; }
		}

		public int MaxConnections {
			get {return max_connections;}
			set {
				if (value < 1)
					throw new ArgumentOutOfRangeException (
						"value",
						Strings.Server_MaxConnsOutOfRange);

				max_connections = value;
			}
		}

		public int ConnectionCount {
			get {
				return connections.Count;
			}
		}

		public GenericServer (Socket socket, IServerCallback<T> callback)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			listen_socket = socket;
			serverCallback = callback;
		}

		public bool Start (bool background, int backlog)
		{
			lock (state_lock) {
				if (Started)
					throw new InvalidOperationException (Strings.Server_AlreadyStarted);

				try {
					listen_socket.Listen (backlog);
				} catch (System.Net.Sockets.SocketException e){
					if (e.ErrorCode == 10013) {
						Logger.Write (LogLevel.Error, "Failed to start server: permission denied for socket {0}", listen_socket);
						return false;
					}
					Logger.Write (LogLevel.Error, "Failed to start server {0}: {1} ({2})", listen_socket, e.Message, e.ErrorCode);
					throw;
				}

				runner = new Thread (RunServer) { IsBackground = background };
				runner.Start ();

				stopped = false;
			}

			return true;
		}

		public void Stop ()
		{
			// Avoid acquiring the lock if unneeded
			if (stopped)
				return;

			lock (state_lock) {
				if (stopped)
					return;

				if (!Started)
					throw new InvalidOperationException (Strings.Server_NotStarted);

				Started = false;
				stopped = true;

				Logger.Write (LogLevel.Debug, "Stopping now");

				listen_socket.Close ();
				lock (connections_lock)
					foreach (T c in new List<T> (connections))
						EndConnection (c);

				stop_signal.Set ();
				stopped_signal.WaitOne ();

				runner = null;
			}
		}

		public void EndConnection (T connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");

			connection.Stop ();

			lock (connections_lock) {
				if (connections.Contains (connection))
					connections.Remove (connection);
			}

			if (!accepting && CanAccept)
				BeginAccept ();
		}

		void RunServer ()
		{
			Logger.Write (LogLevel.Debug, "Server started [callback: {0}]", serverCallback);
			lock (state_lock) {
				Started = true;
			}
			listen_socket.BeginAccept (OnAccept, null);
			if (runner.IsBackground)
				return;

			stop_signal.WaitOne ();
			stopped_signal.Set ();
		}

		void OnAccept (IAsyncResult ares)
		{
			Logger.Write (LogLevel.Debug, Strings.Server_Accepting);
			T connection = default (T);
			bool created = false;

			lock (accept_lock) {
				accepting = false;
			}

			try {
				try {
					Socket accepted = listen_socket.EndAccept (ares);
					if (stopped) {
						Logger.Write (LogLevel.Debug, "Shutting down...");
						accepted.Close();
						return;
					}
					connection = serverCallback.OnAccept (accepted);
					created = true;
					lock (connections_lock)
						connections.Add (connection);
					connection.RequestReceived += RequestReceived;
				} catch (System.Net.Sockets.SocketException e) {
					Logger.Write (LogLevel.Error,
					              Strings.Server_AcceptFailed, e.Message);
					if (e.ErrorCode == 10022)
						Stop ();
				} catch (ObjectDisposedException) {
					Logger.Write (LogLevel.Debug, Strings.Server_ConnectionClosed);
					return; // Already done (e.g., shutdown)
				}

				if (CanAccept)
					BeginAccept ();
			} catch (Exception e) {
				Logger.Write (LogLevel.Error,
				              Strings.Server_AcceptFailed, e.Message);
				throw;
			}

			if (!created)
				return;
			try {
				connection.Run ();
			} catch (Exception e) {
				Logger.Write (LogLevel.Error, Strings.Server_ConnectionFailed);
				Logger.Write (e);
				try {
					// Upon catastrophic failure, forcefully stop 
					// all remaining connection activity, since no 
					// specific error-handling kicked in to rescue 
					// the connection or its requests and the 
					// connection's main loop has now terminated.
					// This prevents abandoned FastCGI connections 
					// from staying open indefinitely.
					EndConnection (connection);
					Logger.Write (LogLevel.Debug, Strings.Server_ConnectionClosed);
				} catch {
					// Ignore at this point -- too bad
				}
			}
		}

		void BeginAccept ()
		{
			lock (accept_lock) {
				if (accepting)
					return;

				accepting = true;
				listen_socket.BeginAccept (OnAccept, null);
			}
		}
	}
}

