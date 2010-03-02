//
// Mono.WebServer.Worker
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// Documentation:
//	Brian Nickel
//
// (C) Copyright 2004-2010 Novell, Inc. (http://www.novell.com)
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

namespace Mono.WebServer
{
	/// <summary>
	///    This abstract is used for running implementation specific
	///    requests.
	/// </summary>
	public abstract class Worker
	{
		/// <summary>
		///    Gets whether or not the current instance is asynchronous.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not the
		///    current instance is asynchronous.
		/// </value>
		/// <remarks>
		///    This property indicates whether or not a call to <see
		///    cref="Run" /> will spawn its own worker thread. <see
		///    langword="false" /> indicates that the entire process
		///    will be completed in a single thread during the duration
		///    of <see cref="Run" />.
		/// </remarks>
		public virtual bool IsAsync {
			get { return false; }
		}

		/// <summary>
		///    Sets the number of times the current instance has been
		///    reused by the server.
		/// </summary>
		/// <param name="reuses">
		///    A <see cref="int" /> containing the number of times the
		///    current instance has been reused.
		/// </param>
		public virtual void SetReuseCount (int reuses)
		{
		}

		/// <summary>
		///    Gets the number of times the current instance can be
		///    reused by the server.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> containing the number of times the
		///    current instance can be reused by the server.
		/// </returns>
		/// <remarks>
		///    If less than or equal to zero, the current instance
		///    should not be reused and the resources associated with it
		///    should be freed.
		/// </remarks>
		public virtual int GetRemainingReuses ()
		{
			return 0;
		}

		/// <summary>
		///    Runs the current instance.
		/// </summary>
		/// <param name="state">
		///    A <see cref="object" /> containing state information from
		///    the worker that evoked the method. Always <see
		///    langref="null" />.
		/// </param>
		/// <remarks>
		///    If the entire process of running the request is done in
		///    the method, <see cref="IsAsync" /> should be set to <see
		///    langword="false" />. If, however, the method evokes an
		///    asynchronous or threaded call, like <see
		///    cref="Socket.BeginReceive" />, <see cref="IsAsync" />
		///    should be set to <see langword="true" />.
		/// </remarks>
		public abstract void Run (object state);
		
		/// <summary>
		///    Reads a block of request data from the current
		///    implementation.
		/// </summary>
		/// <param name="buffer">
		///    A <see cref="byte[]" /> to be populated with the read
		///    data.
		/// </param>
		/// <param name="position">
		///    A <see cref="int" /> containing the position in <paramref
		///    name="buffer" /> it which to start storing the read data.
		/// </param>
		/// <param name="size">
		///    A <see cref="int" /> containing the number of bytes to
		///    read.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> containing the number of bytes that
		///    were actually read.
		/// </returns>
		public abstract int Read (byte [] buffer, int position, int size);
		
		/// <summary>
		///    Writes a block of response data from the current
		///    implementation.
		/// </summary>
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
		public abstract void Write (byte [] buffer, int position, int size);
		
		/// <summary>
		///    Closes the current instance and releases the resources
		///    associated with the data transfer.
		/// </summary>
		public abstract void Close ();
		
		/// <summary>
		///    Causes all response data to be written.
		/// </summary>
		public abstract void Flush ();
		
		/// <summary>
		///    Gets whether or not the current instance is connected.
		/// </summary>
		/// <returns>
		///    A <see cref="bool" /> indicating whether or not the
		///    current instance is connected.
		/// </returns>
		public abstract bool IsConnected ();
	}
}
