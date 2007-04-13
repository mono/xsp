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
	public class BaseRequestBroker: MarshalByRefObject, IRequestBroker
	{
		const int INITIAL_REQUESTS = 200;
		static object reqlock = new object();		

		bool[] request_ids = new bool [INITIAL_REQUESTS];
		Worker[] requests = new Worker [INITIAL_REQUESTS];
		byte[][] buffers = new byte [INITIAL_REQUESTS][];
		int requests_count = 0;		

		// this *MUST* be called with the reqlock held!
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
		
		// this *MUST* be called with the reqlock held!
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
		
		public int RegisterRequest (Worker worker)
		{
			int result = -1;
			
			lock (reqlock) {
				result = GetNextRequestId ();
				requests [result] = worker;
				buffers [result] = new byte [16384];
			}

			return result;
		}
		
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

		protected bool ValidRequest (int requestId)
		{
			return (requestId >= 0 && requestId < request_ids.Length && request_ids [requestId] &&
				buffers [requestId] != null);
		}
		
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
		
		public Worker GetWorker (int requestId)
		{
			if (!ValidRequest (requestId))
				return null;
			
			lock (reqlock) {
				return (Worker) requests [requestId];
			}
		}
		
		public void Write (int requestId, byte[] buffer, int position, int size)
		{
			Worker worker = GetWorker (requestId);
			if (worker != null)
				worker.Write (buffer, position, size);
		}
		
		public void Close (int requestId)
		{
			Worker worker = GetWorker (requestId);
			if (worker != null)
				worker.Close ();
		}
		
		public void Flush (int requestId)
		{
			Worker worker = GetWorker (requestId);
			if (worker != null)
				worker.Flush ();
		}

		public bool IsConnected (int requestId)
		{
			Worker worker = GetWorker (requestId);
			
			return (worker != null && worker.IsConnected ());
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}

