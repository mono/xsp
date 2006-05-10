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
		Hashtable requests = new Hashtable ();
		Hashtable buffers = new Hashtable ();

		static Stack stk = new Stack ();
		static byte [] Allocate16k ()
		{
			if (stk.Count > 0)
				return (byte []) stk.Pop ();

			return new byte [16384];
		}

		static void ReleaseBuffer (byte [] buffer)
		{
			stk.Push (buffer);
		}

		public int RegisterRequest (Worker worker)
		{
			int result = worker.GetHashCode ();
			lock (requests) {
				requests [result] = worker;
				buffers [result] = Allocate16k ();
			}

			return result;
		}
		
		public void UnregisterRequest (int id)
		{
			lock (requests) {
				requests.Remove (id);
				ReleaseBuffer ((byte []) buffers [id]);
				buffers.Remove (id);
			}
		}

		public int Read (int requestId, int size, out byte[] buffer)
		{
			if (size == 16384) {
				buffer = (byte []) buffers [requestId];
			} else {
				buffer = new byte[size];
			}
			Worker w;
			lock (requests) {
				w = (Worker) requests [requestId];
			}

			int nread = 0;
			if (w != null)
				nread = w.Read (buffer, 0, size);

			return nread;
		}
		
		public Worker GetWorker (int requestId)
		{
			lock (requests) {
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
			return (GetWorker (requestId)).IsConnected ();
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}

