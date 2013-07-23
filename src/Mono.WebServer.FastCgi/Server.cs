//
// Server.cs: Accepts connections.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using Mono.WebServer.FastCgi;
using Mono.WebServer.Log;

namespace Mono.FastCgi {
	public class Server
	{
		public BufferManager BigBufferManager { get; private set; }
		public BufferManager SmallBufferManager { get; private set; }

		#region Private Fields

		readonly List<Connection> connections = new List<Connection> ();
		
		readonly Socket listen_socket;
		
		bool started;
		
		bool accepting;

		bool stopped;
		
		Thread runner;
		
		readonly object accept_lock = new object ();
		
		int max_connections = Int32.MaxValue;
		
		int max_requests = Int32.MaxValue;
		
		bool multiplex_connections;
		
		Type responder_type;

		readonly object state_lock = new object ();
		
		#endregion
		
		
		
		#region Constructors
		
		public Server (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			
			listen_socket = socket;

			BigBufferManager = new BufferManager (4 * 1024); //4k
			SmallBufferManager = new BufferManager (8);
		}
		
		#endregion
		
		
		
		#region Public Properties

		public event EventHandler RequestReceived;
		
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
		
		public bool MultiplexConnections {
			get {return multiplex_connections;}
			set {multiplex_connections = value;}
		}
		
		public bool CanAccept {
			get {return started && ConnectionCount < max_connections;}
		}
		
		public bool CanRequest {
			get {return started && RequestCount < max_requests;}
		}
		
		public int ConnectionCount {
			get {
				lock (connections)
					return connections.Count;
			}
		}
		
		public int RequestCount {
			get {
				int requests = 0;
				lock (connections) {
					requests += connections.Sum(c => c.RequestCount);
				}
				
				return requests;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		[Obsolete]
		public void Start (bool background)
		{
			Start (background, 500);
		}

		public void Start (bool background, int backlog)
		{
			lock (state_lock) {
				stopped = false;

				if (started) {
					throw new InvalidOperationException (
						Strings.Server_AlreadyStarted);
				}

				listen_socket.Listen (backlog);

				runner = new Thread (RunServer) { IsBackground = background };
				runner.Start ();
			}
		}

		/// <summary>
		/// Stop this instance. Calling Stop multiple times is a no-op.
		/// </summary>
		public void Stop ()
		{
			lock (state_lock) {
				if (stopped)
					return;

				if (!started)
					throw new InvalidOperationException (Strings.Server_NotStarted);

				started = false;
				stopped = true;

				listen_socket.Close ();
				lock (connections) {
					foreach (Connection c in new List<Connection> (connections)) {
						EndConnection (c);
					}
				}
			
				runner.Abort ();
				runner = null;
			}
		}
		
		public void EndConnection (Connection connection)
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
		
		
		public IDictionary<string,string> GetValues (IEnumerable<string> names)
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			var pairs = new Dictionary<string,string> ();
			foreach (string name in names) {
				// We can't handle null values and we don't need
				// to store the same value twice.
				if (name == null || pairs.ContainsKey (name))
					continue;
				
				string value = null;
				switch (name)
				{
				case "FCGI_MAX_CONNS":
					value = max_connections.ToString (
						CultureInfo.InvariantCulture);
				break;
					
				case "FCGI_MAX_REQS":
					value = max_requests.ToString (
						CultureInfo.InvariantCulture);
				break;
				
				case "FCGI_MPXS_CONNS":
					value = multiplex_connections ? "1" : "0";
				break;
				}
				
				if (value == null) {
					Logger.Write (LogLevel.Warning,
						Strings.Server_ValueUnknown,
						name);
					continue;
				}
				
				pairs.Add (name, value);
			}
			
			return pairs;
		}

		[Obsolete("Use BigBufferManager or SmallBufferManager instead.")]
		public void AllocateBuffers (out byte [] buffer1,
		                             out byte [] buffer2)
		{
			buffer1 = new byte[Record.SuggestedBufferSize];
			buffer2 = new byte[Record.SuggestedBufferSize];
		}

		[Obsolete("Use BigBufferManager or SmallBufferManager instead.")]
		public void ReleaseBuffers (byte [] buffer1, byte [] buffer2)
		{
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		void RunServer ()
		{
			started = true;
			listen_socket.BeginAccept (OnAccept, null);
			if (runner.IsBackground)
				return;

			while (true) // Just sleep until we're aborted.
				Thread.Sleep (1000000);
		}
		
		void OnAccept (IAsyncResult ares)
		{
			Logger.Write (LogLevel.Debug, Strings.Server_Accepting);
			Connection connection = null;
			
			lock (accept_lock) {
				accepting = false;
			}
			
			try {
				try {
					Socket accepted = listen_socket.EndAccept (ares);
					connection = new Connection (accepted, this);
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
			
			if (connection == null)
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
		
		#endregion
		
		
		
		#region Responder Management

		public void SetResponder (Type responder)
		{
			if (responder == null)
				throw new ArgumentNullException ("responder");

			if (!typeof (IResponder).IsAssignableFrom (responder))
				throw new ArgumentException (
					Strings.Server_ResponderDoesNotImplement,
					"responder");
			
			// Checks that the correct constructor is available.
			if (responder.GetConstructor (new[]
				{typeof (ResponderRequest)}) == null) {
				
				string msg = String.Format (
					CultureInfo.CurrentCulture,
					Strings.Server_ResponderLacksProperConstructor,
					responder);
				
				throw new ArgumentException (msg, "responder");
			}
			
			responder_type = responder;
		}
		
		public IResponder CreateResponder (ResponderRequest request)
		{
			if (!SupportsResponder)
				throw new InvalidOperationException (
					Strings.Server_ResponderNotSupported);
			
			return (IResponder) Activator.CreateInstance
				(responder_type, new object [] {request});
		}
		
		public bool SupportsResponder {
			get {return responder_type != null;}
		}
		
		#endregion
	}
}
