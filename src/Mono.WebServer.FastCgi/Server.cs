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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mono.WebServer.FastCgi;
using Mono.WebServer.Log;

namespace Mono.FastCgi {
	public class Server
	{
		public BufferManager BigBufferManager { get; private set; }
		public BufferManager SmallBufferManager { get; private set; }

		#region Private Fields

		readonly IGenericServer<ConnectionProxy> backend;
		
		int max_requests = Int32.MaxValue;
		
		Type responder_type;
		
		#endregion

		
		#region Constructors
		
		public Server (Socket socket)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");

			BigBufferManager = new BufferManager (4 * 1024); //4k
			SmallBufferManager = new BufferManager (8);

			backend = new GenericServer<ConnectionProxy> (socket, new ServerProxy (this));
			backend.RequestReceived += RequestReceived;
		}

		public Server (IGenericServer<ConnectionProxy> backend)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");

			BigBufferManager = new BufferManager (4 * 1024); //4k
			SmallBufferManager = new BufferManager (8);

			this.backend = backend;
			backend.RequestReceived += RequestReceived;
		}
		
		#endregion
		
		
		
		#region Public Properties

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
		
		public bool CanAccept {
			get {return backend.CanAccept; }
		}
		
		public bool CanRequest {
			get {return backend.Started && RequestCount < max_requests;}
		}
		
		public int ConnectionCount {
			get {
				return backend.ConnectionCount;
			}
		}
		
		public int RequestCount {
			get {
				return backend.Connections.Sum(c => c.RequestCount);
			}
		}
		
		#endregion
		
		
		
		#region Public Methods

		internal ConnectionProxy OnAccept (Socket socket)
		{
			return new ConnectionProxy (new Connection (socket, this));
		}
		
		[Obsolete]
		public void Start (bool background)
		{
			Start (background, 500);
		}

		public void Start (bool background, int backlog)
		{
			backend.Start (background, backlog);
		}

		/// <summary>
		/// Stop this instance. Calling Stop multiple times is a no-op.
		/// </summary>
		public void Stop ()
		{
			backend.Stop ();
		}
		
		public void EndConnection (Connection connection)
		{
			backend.EndConnection (new ConnectionProxy (connection));
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
					value = backend.MaxConnections.ToString (CultureInfo.InvariantCulture);
				break;
					
				case "FCGI_MAX_REQS":
					value = max_requests.ToString (
						CultureInfo.InvariantCulture);
				break;
				
				case "FCGI_MPXS_CONNS":
					value = MultiplexConnections ? "1" : "0";
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
