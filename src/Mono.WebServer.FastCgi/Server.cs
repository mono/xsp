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
#if NET_2_0
using System.Collections.Generic;
#else
using System.Collections;
#endif
using System.Globalization;

namespace Mono.FastCgi {
	/// <summary>
	///    This class runs a FastCGI server and registers responder types.
	/// </summary>
	public class Server
	{
		#region Private Fields
		
		/// <summary>
		///    Contains a list of the current connections.
		/// </summary>
		#if NET_2_0
		private List<Connection> connections = new List<Connection> ();
		#else
		private ArrayList connections = new ArrayList ();
		#endif
		
		/// <summary>
		///    Contains the socket to listen on.
		/// </summary>
		private Socket listen_socket;
		
		/// <summary>
		///    Indicates whether or not the server is running.
		/// </summary>
		private bool started = false;
		
		/// <summary>
		///    Indicates whether or not the server is currently
		///    accepting a connection.
		/// </summary>
		private bool accepting = false;
		
		/// <summary>
		///    Contains the thread used to create the server and
		///    optionally keep it alive.
		/// </summary>
		private Thread runner;
		
		/// <summary>
		///    Contains the lock used to regulate the accepting of
		///    sockets.
		/// </summary>
		private object accept_lock = new object ();
		
		/// <summary>
		///    Contains the async callback to be called when a socket is
		///    accepted.
		/// </summary>
		private AsyncCallback accept_cb;
		
		/// <summary>
		///    Contains the maximum number of connections to permit, as
		///    mandated by the FastCGI specification.
		/// </summary>
		private int  max_connections = int.MaxValue;
		
		/// <summary>
		///    Contains the maximum number of requests to permit, as
		///    mandated by the FastCGI specification.
		/// </summary>
		private int  max_requests = int.MaxValue;
		
		/// <summary>
		///    Indicates whether or not to multiplex connections to
		///    permit, as mandated by the FastCGI specification.
		/// </summary>
		private bool multiplex_connections = false;
		
		/// <summary>
		///    Contains the type of class to use for handling the
		///    responder role.
		/// </summary>
		private System.Type responder_type = null;
		
		/// <summary>
		///    Contains buffers to use for new connections.
		/// </summary>
		private byte [][] buffers = new byte [200][];
		
		/// <summary>
		///    Contains the number of buffers stored in <see
		///    cref="buffers" />.
		/// </summary>
		private int buffer_count;
		
		/// <summary>
		///    Contains the lock to use when allocating and releasing
		///    buffers.
		/// </summary>
		private object buffer_lock = new object ();
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of
		///    <see cref="Server" /> with a given socket.
		/// </summary>
		/// <param name="socket">
		///    A <see cref="Socket" /> object to listen on.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="socket" /> is <see langword="null" />.
		/// </exception>
		public Server (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			
			this.listen_socket = socket;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets the maximum number of concurrent
		///    connections the current instance will allow before it
		///    stops accepting new ones.
		/// </summary>
		/// <value>
		///    A <see cref="Int32" /> containing the number of
		///    concurrent connections allowed by the current instance.
		/// </value>
		/// <remarks>
		///    When the maximum number of connections has been reached,
		///    the server will stop accepting connections until one of
		///    the existing connection is terminated.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		///    The value is less than 1.
		/// </exception>
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
		
		/// <summary>
		///    Gets and sets the maximum number of concurrent requests
		///    the current instance will allow before it starts
		///    rejecting new ones.
		/// </summary>
		/// <value>
		///    A <see cref="Int32" /> containing the number of
		///    concurrent requests allowed by the current instance.
		/// </value>
		/// <remarks>
		///    <para>When the maximum number of requests has been
		///    reached, the server will respond to requests with the
		///    FastCGI "Overloaded" end-of-request record.</para>
		///    <para>In the case the connection multiplexing is
		///    disabled, this property is redundant to
		///    <see cref="MaxConnections" />, as only one request is
		///    permitted per connection. In such a case, this property
		///    should be no less than <see cref="MaxConnections" /> to
		///    avoid unnecessary connections.</para>
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		///    The value is less than 1.
		/// </exception>
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
		
		/// <summary>
		///    Gets and sets whether or not the multiplexing of
		///    requests is permitted in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="Boolean" /> indicating whether or not
		///    multiplexing is permitted in the current instance.
		/// </value>
		/// <remarks>
		///    Multiplexing of connections allows multiple requests and
		///    responses to be sent simultaneously. This allows for
		///    improved response times for multiple requests send over a
		///    single connection.
		/// </remarks>
		public bool MultiplexConnections {
			get {return multiplex_connections;}
			set {multiplex_connections = value;}
		}
		
		/// <summary>
		///    Gets whether or not the current instance can accept
		///    another connection.
		/// </summary>
		/// <value>
		///    A <see cref="Boolean" /> indicating whether or not
		///    the current instance will permit another connection.
		/// </value>
		public bool CanAccept {
			get {return started && ConnectionCount < max_connections;}
		}
		
		/// <summary>
		///    Gets whether or not the current instance can accept
		///    another request.
		/// </summary>
		/// <value>
		///    A <see cref="Boolean" /> indicating whether or not
		///    the current instance will permit another request.
		/// </value>
		public bool CanRequest {
			get {return started && RequestCount < max_requests;}
		}
		
		/// <summary>
		///    Gets the total number of open connections managed by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="Int32" /> containing the total number of
		///    open connections managed by the current instance.
		/// </value>
		public int ConnectionCount {
			get {return connections.Count;}
		}
		
		/// <summary>
		///    Gets the total number of open requests managed by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="Int32" /> containing the total number of
		///    open requests managed by the current instance.
		/// </value>
		public int RequestCount {
			get {
				int requests = 0;
				foreach (Connection c in connections)
					requests += c.RequestCount;
				
				return requests;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Starts the server in a different thread.
		/// </summary>
		/// <param name="background">
		///    A <see cref="Boolean" /> specifying whether or not the
		///    server thread should be run as a background thread.
		/// </param>
		/// <remarks>
		///    <para>The behavior of background and foreground threads
		///    are identical except in that fact that the application
		///    will not terminate while foreground threads are
		///    running.</para>
		///    <para>See <see cref="Thread.IsBackground" /> for more
		///    details.</para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///    The server is already started.
		/// </exception>
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
		
		/// <summary>
		///    Stops the server.
		/// </summary>
		/// <remarks>
		///    This closes all connections and aborts the thread. If
		///    the thread is a foreground thread, this will allow the
		///    program to terminate.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///    The server is not started.
		/// </exception>
		public void Stop ()
		{
			if (!started)
				throw new InvalidOperationException (
					Strings.Server_NotStarted);

			started = false;
			listen_socket.Close ();
			#if NET_2_0
			foreach (Connection c in new List<Connection> (
				connections)) {
			#else
			foreach (Connection c in new ArrayList (connections)) {
			#endif
				EndConnection (c);
			}
			
			runner.Abort ();
			runner = null;
		}
		
		/// <summary>
		///    Ends a specified connection.
		/// </summary>
		/// <param name="connection">
		///    A <see cref="Connection" /> object to terminate.
		/// </param>
		/// <remarks>
		///    <para>This method stops a connection by closing its
		///    requests and listening sockets, permitting its thread to
		///    terminate.</para>
		///    <para>Once the connection is stopped, it is removed from
		///    the list of managed connections, and if the server is not
		///    accepting, begins the connection process.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="connection" /> is
		///    <see langword="null" />.
		/// </exception>
		public void EndConnection (Connection connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			
			connection.Stop ();
			
			if (connections.Contains (connection))
				connections.Remove (connection);
			
			if (!accepting && CanAccept)
				BeginAccept ();
		}
		
		
		/// <summary>
		///    Gets name/value pairs for server variables.
		/// </summary>
		/// <param name="names">
		#if NET_2_0
		///    A <see cref="T:System.Collections.Generic.IEnumerable&lt;string&gt;" />
		#else
		///    A <see cref="IEnumerable" />
		#endif
		///    object containing FastCGI server variable names.
		/// </param>
		/// <returns>
		#if NET_2_0
		///    A <see cref="T:System.Collections.Generic.IDictionary&lt;string,string&gt;" />
		#else
		///    A <see cref="IDictionary" />
		#endif
		///    object containing the server variables used by the
		///    current instance.
		/// </returns>
		/// <remarks>
		///    <para>A FastCGI client can at any time request
		///    information on a collection of server variables. It
		///    provides a list of variable names to which the server
		///    responds with name/value pairs containing their
		///    content.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="names" /> is
		///    <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="names" /> contains a non-string value.
		/// </exception>
		#if NET_2_0
		public IDictionary<string,string> GetValues (IEnumerable<string>
		                                             names)
		#else
		public IDictionary GetValues (IEnumerable names)
		#endif
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			#if NET_2_0
			Dictionary<string,string> pairs =
				new Dictionary<string,string> ();
			foreach (string key in names) {
			#else
			Hashtable pairs = new Hashtable ();
			foreach (object key in names) {
			#endif
				
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
		
		/// <summary>
		///    Allocates two buffers from the current instance for use
		///    in sending and receiving records.
		/// </summary>
		/// <param name="buffer1">
		///    A <see cref="byte[]" /> of size <see
		///    cref="Record.SuggestedBufferSize" />.
		/// </param>
		/// <param name="buffer2">
		///    A <see cref="byte[]" /> of size <see
		///    cref="Record.SuggestedBufferSize" />.
		/// </param>
		/// <remarks>
		///    The current instance manages buffers to improve
		///    performance. To release buffers back to the current
		///    instance, use <see cref="ReleaseBuffers" />.
		/// </remarks>
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
		
		/// <summary>
		///    Releases two buffers back to the current instance.
		/// </summary>
		/// <param name="buffer1">
		///    A <see cref="byte[]" /> allocated by <see
		///    cref="AllocateBuffers" />.
		/// </param>
		/// <param name="buffer2">
		///    A <see cref="byte[]" /> allocated by <see
		///    cref="AllocateBuffers" />.
		/// </param>
		/// <remarks>
		///    The current instance manages buffers to improve
		///    performance. To allocate buffers, use <see
		///    cref="AllocateBuffers" />.
		/// </remarks>
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
		
		/// <summary>
		///    Starts the server by beginning an accept call.
		/// </summary>
		/// <remarks>
		///    If the server is running in the foreground, the thread 
		///    loops, waiting to be aborted.
		/// </remarks>
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
		
		/// <summary>
		///    Accepts a connection.
		/// </summary>
		/// <param name="ares">
		///    A <see cref="IAsyncResult" /> containing the results of
		///    the accept call.
		/// </param>
		/// <remarks>
		///    <para>Upon accepting the connection, it attempts to
		///    create a <see cref="Connection" /> object, attempts to
		///    start the accept process again, and then runs the
		///    connection.</para>
		///    <para>The thread that evoked this method is used for the
		///    duration of the connection.</para>
		/// </remarks>
		private void OnAccept (IAsyncResult ares)
		{
			Logger.Write (LogLevel.Debug, Strings.Server_Accepting);
			Connection connection = null;
			
			lock (accept_lock) {
				accepting = false;
			}
			
			try {
				Socket accepted = listen_socket.EndAccept (ares);
				connection = new Connection (accepted, this);
				connections.Add (connection);
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error,
					Strings.Server_AcceptFailed, e.Message);
				if (e.ErrorCode == 10022)
					Stop ();
			}
			
			if (CanAccept)
				BeginAccept ();
			
			if (connection != null)
				connection.Run ();
		}
		
		/// <summary>
		///    Begins accepting a connection, unless one is already
		///    being accepted.
		/// </summary>
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
		
		/// <summary>
		///    Sets the <see cref="IResponder" /> type to use for the
		///    FastCGI responder role.
		/// </summary>
		/// <param name="responder">
		///    A <see cref="Type" /> for a class implementing the
		///    <see cref="IResponder" /> interface.
		/// </param>
		/// <exception cref="ArgumentException">
		///    <paramref name="responder" /> does not implement the
		///    <see cref="IResponder" /> interface or does not provide
		///    the proper constructor.
		/// </exception>
		public void SetResponder (System.Type responder)
		{
			if (responder == null) {
				responder_type = responder;
				return;
			}
			
			int i = 0;
			System.Type [] faces = responder.GetInterfaces ();
			System.Type iresp_type = typeof (IResponder);
			while (i < faces.Length && faces [i] != iresp_type)
				i ++;
			
			// If the list was looped through completely, the
			// IResponder interface was not found.
			if (i == faces.Length)
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
		
		/// <summary>
		///    Creates a new <see cref="IResponder" /> object for a
		///    specified request.
		/// </summary>
		/// <param name="request">
		///    A <see cref="ResponderRequest" /> object to create a
		///    responder for.
		/// </param>
		/// <returns>
		///    A <see cref="IResponder" /> object for the provided
		///    request.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///    The responder role is not supported.
		/// </exception>
		public IResponder CreateResponder (ResponderRequest request)
		{
			if (!SupportsResponder)
				throw new InvalidOperationException (
					Strings.Server_ResponderNotSupported);
			
			return (IResponder) Activator.CreateInstance
				(responder_type, new object [] {request});
		}
		
		/// <summary>
		///    Gets whether or not the current instance supports
		///    the role of FastCGI responder.
		/// </summary>
		/// <value>
		///    A <see cref="Boolean" /> indicating whether or not the
		///    responder role is supported by the current instance.
		/// </value>
		public bool SupportsResponder {
			get {return responder_type != null;}
		}
		
		#endregion
	}
}
