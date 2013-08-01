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
using System.Collections.ObjectModel;
using System.Threading;
using Mono.FastCgi;
using Mono.WebServer.Log;

namespace Mono.WebServer.FastCgi
{
	public class GenericServer<T> : IGenericServer<T> where T : IConnection
	{
		readonly Socket listen_socket;
		readonly IServerCallback<T> serverCallback;
		readonly object accept_lock = new object();
		readonly object state_lock = new object ();
		bool accepting;
		bool stopped;
		Thread runner;
		readonly List<T> connections = new List<T>();
		int max_connections = Int32.MaxValue;
		ManualResetEvent stop_signal = new ManualResetEvent(false);
		ManualResetEvent stopped_signal = new ManualResetEvent(false);

		public event EventHandler RequestReceived;

		public IEnumerable<T> Connections {
			get { return connections; }
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
				lock (connections)
					return connections.Count;
			}
		}

		public GenericServer (Socket socket, IServerCallback<T> callback)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			if(callback==null)
				throw new ArgumentNullException("callback");
			listen_socket = socket;
			serverCallback = callback;
		}

		public void Start (bool background, int backlog)
		{
			lock (state_lock) {
				if (Started)
					throw new InvalidOperationException (Strings.Server_AlreadyStarted);

				listen_socket.Listen (backlog);

				runner = new Thread (RunServer) { IsBackground = background };
				runner.Start ();

				stopped = false;
			}
		}

		public void Stop ()
		{
			lock (state_lock) {
				if (stopped)
					return;

				if (!Started)
					throw new InvalidOperationException (Strings.Server_NotStarted);

				listen_socket.Close ();
				lock (connections) {
					foreach (T c in new List<T> (connections)) {
						EndConnection (c);
					}
				}

				stop_signal.Set ();
				stopped_signal.WaitOne ();
				stopped_signal.Reset ();

				runner = null;

				Started = false;
				stopped = true;
			}
		}

		public void EndConnection (T connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");

			connection.Stop ();

			lock (connections) {
				if (connections.Contains (connection))
					connections.Remove (connection);
			}

			if (!accepting && CanAccept)
				BeginAccept ();
		}

		void RunServer ()
		{
			lock (state_lock) {
				Started = true;
			}
			listen_socket.BeginAccept (OnAccept, null);
			if (runner.IsBackground)
				return;

			stop_signal.WaitOne ();
			stop_signal.Reset ();
			stopped_signal.Set ();
		}

		void OnAccept (IAsyncResult ares)
		{
			Logger.Write (LogLevel.Debug, Strings.Server_Accepting);
			T connection = default(T);
			bool created = false;

			lock (accept_lock) {
				accepting = false;
			}

			try {
				try {
					Socket accepted = listen_socket.EndAccept (ares);
					connection = serverCallback.OnAccept (accepted);
					created = true;
					lock (connections)
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
					EndConnection(connection);
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

