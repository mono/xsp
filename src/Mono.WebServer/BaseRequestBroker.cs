
//
// Mono.WebServer.BaseRequestBroker
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
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
using System.Collections;

namespace Mono.WebServer
{
	/// <summary>
	///   This class extends <see cref="EventArgs"/> to provide arguments for
	///   <see cref="BaseRequestBroker.UnregisterRequestEventHandler"/>.
	/// </summary>
	public class UnregisterRequestEventArgs : EventArgs
	{
		int _requestId;

		/// <summary>
		///   Contains the id of a request that is about to be unregistered.
		/// </summary>
		public int RequestId {
			get { return _requestId; }
		}

		/// <summary>
		///   Constructs an instance of the class for the specified request ID
		/// </summary>
		/// <param name="requestId">Request of the ID that has just been unregistered</param>
		public UnregisterRequestEventArgs (int requestId)
		{
			_requestId = requestId;
		}
	}
	
	/// <summary>
	///    This class provides a request broker covering the base
	///    functionality.
	/// </summary>
	/// <remarks>
	///    A request broker serves as an intermediary between <see
	///    cref="Worker" /> and <see cref="MonoWorkerRequest" /> to handle
	///    the interaction between app-domains.
	/// </remarks>
	public class BaseRequestBroker: MarshalByRefObject, IRequestBroker
	{
		/// <summary>
		///   This delegate is used to handle <see cref="UnregisterRequestEvent"/>
		/// </summary>
		/// <param name="sender">Origin of the event</param>
		/// <param name="args">An <see cref="UnregisterRequestEventArgs"/> object with the event-specific arguments</param>
		public delegate void UnregisterRequestEventHandler (object sender, UnregisterRequestEventArgs args);

		/// <summary>
		///   This event is called just before the request is unregistered by the broker.
		///   This gives the chance to clean up any private data associated with the event.
		/// </summary>
		/// <remarks>
		///   The event handlers are invoked with a lock held on the issuing object, so that the event receiver
		///   can do the cleanup without the chance of another thread stepping in at the wrong time.
		/// </remarks>
		public event UnregisterRequestEventHandler UnregisterRequestEvent;
		
		/// <summary>
		///    Contains the initial request capacity of a <see
		///    cref="BaseRequestBroker" />.
		/// </summary>
		const int INITIAL_REQUESTS = 200;
		
		/// <summary>
		///    Contains a lock to use when accessing and modifying the
		///     request allocation tables.
		/// </summary>
		static object reqlock = new object();		

		/// <summary>
		///    Contains the request ID's.
		/// </summary>
		int[] request_ids = new int [INITIAL_REQUESTS];

		/// <summary>
		///    Contains the registered workers.
		/// </summary>
		Worker[] requests = new Worker [INITIAL_REQUESTS];
		
		/// <summary>
		///    Contains buffers for the requests to use.
		/// </summary>
		byte[][] buffers = new byte [INITIAL_REQUESTS][];
		
		/// <summary>
		///    Contains the number of active requests.
		/// </summary>
		int requests_count = 0;

		/// <summary>
		///    Contains the total number of requests served so far.
		///    May freely wrap around.
		/// </summary>
		uint requests_served = 0;

		/// <summary>
		///    Grows the size of the request allocation tables by 33%.
		/// </summary>
		/// <param name="curlen">
		///    A <see cref="int" /> containing the current length of the
		///    allocation tables.
		/// </param>
		/// <param name="newid">
		///    A <see cref="int" /> containing the ID to use for a new
		///    request.
		/// </param>
		/// <remarks>
		///    <note type="caution"><para>
		///        This *MUST* be called with the reqlock held!
		///    </para></note>
		/// </remarks>
		void GrowRequests (ref int curlen, ref int newid)
		{
			int newsize = curlen + curlen/3;
			int[] new_request_ids = new int [newsize];
			Worker[] new_requests = new Worker [newsize];
			byte[][] new_buffers = new byte [newsize][];

			request_ids.CopyTo (new_request_ids, 0);
			Array.Clear (request_ids, 0, request_ids.Length);
			request_ids = new_request_ids;
			
			requests.CopyTo (new_requests, 0);
			Array.Clear (requests, 0, requests.Length);
			requests = new_requests;
			
			buffers.CopyTo (new_buffers, 0);
			Array.Clear (buffers, 0, buffers.Length);
			buffers = new_buffers;

			newid = curlen + 1;
			curlen = newsize;
		}
		
		/// <summary>
		///    Gets the next available request ID, expanding the array
		///    of possible ID's if necessary.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> containing the ID of the request.
		/// </returns>
		/// <remarks>
		///    <note type="caution"><para>
		///        This *MUST* be called with the reqlock held!
		///    </para></note>
		/// </remarks>
		int GetNextRequestId ()
		{
			int reqlen = request_ids.Length;
			int newid = -1;
			
			requests_served++; // increment to 1 before putting into request_ids
					   // so that the 0 id is reserved for slot not used
			if (requests_served == 0x8000) // and check for wrap-around for the above
				requests_served = 1; // making sure we don't exceed 0x7FFF or go negative

			requests_count++;
			if (requests_count >= reqlen)
				GrowRequests (ref reqlen, ref newid);
			if (newid == -1)
				for (int i = 0; i < reqlen; i++) {
					if (request_ids [i] == 0) {
						newid = i;
						break;
					}
			}

			if (newid != -1) {
				// TODO: newid had better not exceed 0xFFFF.
				newid = (int)((newid & 0xFFFF) | ((requests_served & 0x7FFF) << 16));
				request_ids [IdToIndex(newid)] = newid;
				return newid;
			}
				
			// Should never happen...
			throw new ApplicationException ("could not allocate new request id");
		}
		
		/// <summary>
		///    Registers a request with the current instance.
		/// </summary>
		/// <param name="worker">
		///    A <see cref="Worker" /> object containing the request to
		///    register.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> containing the ID of the request.
		/// </returns>
		public int RegisterRequest (Worker worker)
		{
			int result = -1;
			
			lock (reqlock) {
				result = IdToIndex (GetNextRequestId ());
				requests [result] = worker;
				
				// Don't create a new array if one already
				// exists.
				byte[] a = buffers [result];
				if (a == null || a.Length != 16384)
					buffers [result] = new byte [16384];
			}

			return request_ids [result];
		}

		private int IdToIndex(int requestId) {
			return requestId & 0xFFFF;
		}

		/// <summary>
		///    Unregisters a request with the current instance.
		/// </summary>
		/// <param name="id">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <remarks>
		///    Before unregistering the request and freeing all of its data, the method
		///    invokes the <see cref="UnregisterRequestEvent"/> handlers (if any).
		///    <note type="caution"><para>
		///       After the event handlers return the request ID is invalid and
		///       *MUST NOT* be used for any purpose other than referencing the event
		///       receiver's internal housekeeping records for that particular ID.
		///    </para></note>
		///    <note type="caution"><para>
		///       Make the event handler code as fast as possible, as until it returns no other
		///       request shall be allocated another id.
		///    </para></note>
		/// </remarks>
		public void UnregisterRequest (int id)
		{
			lock (reqlock) {
				if (!ValidRequest (id))
					return;
				
				DoUnregisterRequest (id);
				int idx = IdToIndex (id);

				byte[] a = buffers [idx];
				if (a != null)
					Array.Clear (a, 0, a.Length);
				requests [idx] = null;
				request_ids [idx] = 0;
				requests_count--;
			}
		}

		/// <summary>
		///    Invokes registered handlers of <see cref="UnregisterRequestEvent"/>. Each handler is
		///    passed an arguments object which contains the ID of a request that is about to be unregistered.
		/// </summary>
		/// <param name="id">ID of a request that is about to be unregistered</param>
		void DoUnregisterRequest (int id)
		{
			if (UnregisterRequestEvent == null)
				return;
			Delegate[] handlers = UnregisterRequestEvent.GetInvocationList ();
			if (handlers == null || handlers.Length == 0)
				return;
			
			UnregisterRequestEventArgs args = new UnregisterRequestEventArgs (id);
			foreach (UnregisterRequestEventHandler handler in handlers)
				handler (this, args);
		}
		
		/// <summary>
		///    Gets whether or not the request with a specified ID is
		///    valid.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> indicating whether or not the
		///    request is valid.
		/// </returns>
		protected bool ValidRequest (int requestId)
		{
			int idx = IdToIndex (requestId);
			return (idx >= 0 && idx < request_ids.Length && request_ids [idx] == requestId &&
				buffers [idx] != null);
		}
		
		/// <summary>
		///    Reads a block of request data from the request with a
		///    specified ID.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <param name="size">
		///    A <see cref="int" /> containing the number of bytes to
		///    read.
		/// </param>
		/// <param name="buffer">
		///    A <see cref="byte[]" /> containing the read data.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> containing the number of bytes that
		///    were actually read.
		/// </returns>
		/// <remarks>
		///    <para>See <see cref="Worker.Read" />.</para>
		/// </remarks>
		public int Read (int requestId, int size, out byte[] buffer)
		{
			buffer = null;
			
			Worker w;

			lock (reqlock) {
				if (!ValidRequest (requestId))
					return 0;

				w = GetWorker (requestId);
				if (w == null)
					return 0;

				if (size == 16384) {
					buffer = buffers [IdToIndex (requestId)];
				} else {
					buffer = new byte[size];
				}
			}

			return w.Read (buffer, 0, size);
		}
		
		/// <summary>
		///    Gets the request with a specified ID.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <returns>
		///    A <see cref="Worker" /> object containing the request
		///    with the specified ID, or <see langword="null" /> if the
		///    request does not exist.
		/// </returns>
		public Worker GetWorker (int requestId)
		{
			lock (reqlock) {
				if (!ValidRequest (requestId))
					return null;
			
				return (Worker) requests [IdToIndex (requestId)];
			}
		}
		
		/// <summary>
		///    Writes a block of response data to the request with a
		///    specified ID.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <param name="buffer">
		///    A <see cref="byte[]" /> containing data to write.
		/// </param>
		/// <param name="position">
		///    A <see cref="int" /> containing the position in <paramref
		///    name="buffer" /> it which to start writing from.
		/// </param>
		/// <param name="size">
		///    A <see cref="int" /> containing the number of bytes to
		///    write.
		/// </param>
		/// <remarks>
		///    <para>See <see cref="Worker.Write" />.</para>
		///    <para>If the request does not exist, no action is
		///    taken.</para>
		/// </remarks>
		public void Write (int requestId, byte[] buffer, int position, int size)
		{
			Worker worker = GetWorker (requestId);
			if (worker != null)
				worker.Write (buffer, position, size);
		}
		
		/// <summary>
		///    Closes the request with a specified ID.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <remarks>
		///    <para>See <see cref="Worker.Close" />.</para>
		///    <para>If the request does not exist, no action is
		///    taken.</para>
		/// </remarks>
		public void Close (int requestId)
		{
//			Console.WriteLine ("{0}.Close (0x{1})", this, requestId.ToString ("x"));
//			Console.WriteLine (Environment.StackTrace);
			Worker worker = GetWorker (requestId);
			if (worker != null)
				worker.Close ();
		}
		
		/// <summary>
		///    Flushes the request with a specified ID.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <remarks>
		///    <para>See <see cref="Worker.Flush" />.</para>
		///    <para>If the request does not exist, no action is
		///    taken.</para>
		/// </remarks>
		public void Flush (int requestId)
		{
			Worker worker = GetWorker (requestId);
			if (worker != null)
				worker.Flush ();
		}

		/// <summary>
		///    Gets whether or not the request with a specified ID is
		///    connected.
		/// </summary>
		/// <param name="requestId">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> indicating whether or not the
		///    request is connected. If the request doesn't exist, <see
		///    langref="false" /> will be returned.
		/// </returns>
		/// <remarks>
		///    See <see cref="Worker.IsConnected" />.
		/// </remarks>
		public bool IsConnected (int requestId)
		{
			Worker worker = GetWorker (requestId);
			
			return (worker != null && worker.IsConnected ());
		}

		/// <summary>
		///    Obtains a lifetime service object for the current
		///    instance.
		/// </summary>
		/// <returns>
		///    Always <see langword="null" />.
		/// </returns>
		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}

