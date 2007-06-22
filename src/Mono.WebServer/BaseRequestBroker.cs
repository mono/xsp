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
		bool[] request_ids = new bool [INITIAL_REQUESTS];
		
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
			bool[] new_request_ids = new bool [newsize];
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
			
			requests_count++;
			if (requests_count >= reqlen)
				GrowRequests (ref reqlen, ref newid);
			if (newid == -1)
				for (int i = 0; i < reqlen; i++) {
					if (!request_ids [i]) {
						newid = i;
						break;
					}
			}

			if (newid != -1) {
				request_ids [newid] = true;
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
				result = GetNextRequestId ();
				requests [result] = worker;
				
				// Don't create a new array if one already
				// exists.
				byte[] a = buffers [result];
				if (a == null || a.Length != 16384)
					buffers [result] = new byte [16384];
			}

			return result;
		}
		
		/// <summary>
		///    Unregisters a request with the current instance.
		/// </summary>
		/// <param name="id">
		///    A <see cref="int" /> containing the ID of the request.
		/// </param>
		public void UnregisterRequest (int id)
		{
			lock (reqlock) {
				if (id < 0 || id > request_ids.Length - 1)
					return;
				if (!request_ids [id])
					return;
				
				requests [id] = null;
				request_ids [id] = false;
				byte[] a = buffers [id];
				if (a != null)
					Array.Clear (a, 0, a.Length);
				requests_count--;
			}
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
			return (requestId >= 0 && requestId < request_ids.Length && request_ids [requestId] &&
				buffers [requestId] != null);
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
			if (!ValidRequest (requestId))
				return 0;
			
			if (size == 16384) {
				buffer = buffers [requestId];
			} else {
				buffer = new byte[size];
			}
			Worker w;
			lock (reqlock) {
				w = (Worker) requests [requestId];
			}

			int nread = 0;
			if (w != null)
				nread = w.Read (buffer, 0, size);

			return nread;
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
			if (!ValidRequest (requestId))
				return null;
			
			lock (reqlock) {
				return (Worker) requests [requestId];
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

