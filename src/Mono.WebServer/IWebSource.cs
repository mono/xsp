//
// Mono.WebServer.IWebSource
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Net.Sockets;

namespace Mono.WebServer {

	public abstract class WebSource : IDisposable
	{
		public abstract Socket CreateSocket ();
		public abstract Worker CreateWorker (Socket client, ApplicationServer server);
		public abstract Type GetApplicationHostType ();
		public abstract IRequestBroker CreateRequestBroker ();

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
		}
	}
	
	public abstract class Worker
	{
		public virtual bool IsAsync {
			get { return false; }
		}

		public virtual void SetReuseCount (int reuses)
		{
		}

		public virtual int GetRemainingReuses ()
		{
			return 0;
		}

		public abstract void Run (object state);
		public abstract int Read (byte [] buffer, int position, int size);
		public abstract void Write (byte [] buffer, int position, int size);
		public abstract void Close ();
		public abstract void Flush ();
		public abstract bool IsConnected ();
	}
}

