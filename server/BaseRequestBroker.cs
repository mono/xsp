//
// Mono.ASPNET.BaseRequestBroker
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//

using System;
using System.Collections;

namespace Mono.ASPNET
{
	public class BaseRequestBroker: MarshalByRefObject, IRequestBroker
	{
		ArrayList requests = new ArrayList ();
		Queue freeSlots = new Queue ();
		
		internal int RegisterRequest (IWorker worker)
		{
			lock (requests)
			{
				if (freeSlots.Count == 0)
					return requests.Add (worker);
				
				int freeSlot = (int)freeSlots.Dequeue ();
				requests [freeSlot] = worker;
				return freeSlot;
			}
		}
		
		internal void UnregisterRequest (int id)
		{
			lock (requests)
			{
				requests [id] = null;
				freeSlots.Enqueue (id);
			}
		}
		
		public int Read (int requestId, int size, out byte[] buffer)
		{
			buffer = new byte[size];
			IWorker w;
			lock (requests) {
				w = (IWorker) requests [requestId];
			}
			int nread = w.Read (buffer, 0, size);
			return nread;
		}
		
		public IWorker GetWorker (int requestId)
		{
			lock (requests) {
				return (IWorker) requests [requestId];
			}
		}
		
		public void Write (int requestId, byte[] buffer, int position, int size)
		{
			GetWorker (requestId).Write (buffer, position, size);
		}
		
		public void Close (int requestId)
		{
			GetWorker (requestId).Close ();
		}
		
		public void Flush (int requestId)
		{
			GetWorker (requestId).Flush ();
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}
