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
using System.Threading;
using System.Collections.Generic;
using System.Globalization;

namespace Mono.FastCgi {
	public class Server
	{
		#region Private Fields
		
		private List<Connection> connections = new List<Connection> ();
		
		private Socket listen_socket;
		
		private bool started = false;
		
		private bool accepting = false;
		
		private Thread runner;
		
		private object accept_lock = new object ();
		
		private AsyncCallback accept_cb;
		
		private int  max_connections = int.MaxValue;
		
		private int  max_requests = int.MaxValue;
		
		private bool multiplex_connections = false;
		
		private System.Type responder_type = null;
		
		private byte [][] buffers = new byte [200][];
		
		private int buffer_count;
		
		private object buffer_lock = new object ();
		
		#endregion
		
		
		
		#region Constructors
		
		public Server (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			
			this.listen_socket = socket;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
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
					foreach (Connection c in connections)
						requests += c.RequestCount;
				}
				
				return requests;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		public void Start (bool background)
		{
			if (started)
				throw new InvalidOperationException (
					Strings.Server_AlreadyStarted);
			
			listen_socket.Listen (500);
			
			runner = new Thread (new ThreadStart (RunServer));
			runner.IsBackground = background;
			runner.Start ();
		}
		
		public void Stop ()
		{
			if (!started)
				throw new InvalidOperationException (
					Strings.Server_NotStarted);

			started = false;
			listen_socket.Close ();
			lock (connections) {
				foreach (Connection c in new List<Connection> (connections)) {
					EndConnection (c);
				}
			}
			
			runner.Abort ();
			runner = null;
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
			
			Dictionary<string,string> pairs = new Dictionary<string,string> ();
			foreach (string key in names) {				
				string name = key as string;
				
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
						key);
					continue;
				}
				
				pairs.Add (name, value);
			}
			
			return pairs;
		}
		
		public void AllocateBuffers (out byte [] buffer1,
		                             out byte [] buffer2)
		{
			
			buffer1 = null;
			buffer2 = null;
			
			lock (buffer_lock) {
				// If there aren't enough existing buffers,
				// create new ones.
				if (buffer_count < 2)
					buffer1 = new byte [Record.SuggestedBufferSize];
				
				if (buffer_count < 1)
					buffer2 = new byte [Record.SuggestedBufferSize];
				
				// Now that buffer1 and buffer2 may have been
				// assigned to compensate for a lack of buffers
				// in the array, loop through and assign the
				// remaining values.
				int length = buffers.Length;
				for (int i = 0; i < length && (buffer1 == null ||
					buffer2 == null); i ++) {
					if (buffers [i] != null) {
						if (buffer1 == null)
							buffer1 = buffers [i];
						else
							buffer2 = buffers [i];
						
						buffers [i] = null;
						buffer_count --;
					}
				}
			}
		}
		
		public void ReleaseBuffers (byte [] buffer1, byte [] buffer2)
		{
			lock (buffer_lock) {
				int length = buffers.Length;
				foreach (byte [] buffer in new byte [][] {buffer1, buffer2}) {
					if (buffer.Length < Record.SuggestedBufferSize)
						continue;
					
					// If the buffer count is equal to the
					// length of the buffer array, it needs
					// to be enlarged.
					if (buffer_count == length) {
						byte [][] buffers_new = new byte [length + length / 3][];
						buffers.CopyTo (buffers_new, 0);
						buffers = buffers_new;
						buffers [buffer_count++] = buffer;
						
						if (buffer == buffer1)
							buffers [buffer_count++] = buffer2;
						
						return;
					}
					
					for (int i = 0; i < length && buffer != null; i++) {
						if (buffers [i] == null) {
							buffers [i] = buffer;
							buffer_count ++;
							break;
						}
					}
				}
				
			}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		private void RunServer ()
		{
			started = true;
			accept_cb = new AsyncCallback (OnAccept);
			listen_socket.BeginAccept (accept_cb, null);
			if (runner.IsBackground)
				return;

			while (true) // Just sleep until we're aborted.
				Thread.Sleep (1000000);
		}
		
		private void OnAccept (IAsyncResult ares)
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
				} catch (System.Net.Sockets.SocketException e) {
					Logger.Write (LogLevel.Error,
						Strings.Server_AcceptFailed, e.Message);
					if (e.ErrorCode == 10022)
						Stop ();
				} catch (System.ObjectDisposedException) {
					Logger.Write (LogLevel.Debug, Strings.Server_ConnectionClosed);
					return; // Already done (e.g., shutdown)
				}
			
				if (CanAccept)
					BeginAccept ();
			} catch (System.Exception e) {
				Logger.Write (LogLevel.Error,
					Strings.Server_AcceptFailed, e.Message);
			}
			
			if (connection == null)
				return;
			try {
				connection.Run ();
			} catch (System.Exception e) {
				Logger.Write (LogLevel.Error,
					Strings.Server_ConnectionFailed, e.Message);
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
		
		private void BeginAccept ()
		{
			lock (accept_lock) {
				if (accepting)
					return;
				
				accepting = true;
				listen_socket.BeginAccept (accept_cb, null);
			}
		}
		
		#endregion
		
		
		
		#region Responder Management
		
		public void SetResponder (System.Type responder)
		{
			if (responder == null) {
				responder_type = responder;
				return;
			}

			if (!typeof (IResponder).IsAssignableFrom (responder))
				throw new ArgumentException (
					Strings.Server_ResponderDoesNotImplement,
					"responder");
			
			// Checks that the correct constructor is available.
			if (responder.GetConstructor (new System.Type[]
				{typeof (ResponderRequest)}) == null) {
				
				string msg = string.Format (
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
