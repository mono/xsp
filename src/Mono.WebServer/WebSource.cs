//
// Mono.WebServer.WebSource
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
	///    This abstract class produces the implementation specific
	///    components needed to run the <see cref="ApplicationServer" />.
	/// </summary>
	public abstract class WebSource : IDisposable
	{
		/// <summary>
		///    Creates a bound socket to be used for listening for new
		///    connections.
		/// </summary>
		/// <returns>
		///    A <see cref="Socket" /> object containing a socket to be
		///    used for listening for new connections.
		/// </returns>
		public abstract Socket CreateSocket ();
		
		/// <summary>
		///    Creates a worker to use to run a request on a client
		///    socket.
		/// </summary>
		/// <param name="client">
		///    A <see cref="Socket" /> object containing a client
		///    socket accepted from the listen socket created by <see
		///    cref="CreateSocket" />.
		/// </param>
		/// <param name="server">
		///    A <see cref="ApplicationServer" /> object containing the
		///    server that created the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="Worker" /> to use to run a request.
		/// </returns>
		public abstract Worker CreateWorker (Socket client, ApplicationServer server);
		
		/// <summary>
		///    Gets the type of application host to use with the current
		///    instance.
		/// </summary>
		/// <returns>
		///    A <see cref="Type" /> containing the type of application
		///    host to use with the current instance.
		/// </returns>
		/// <remarks>
		///    <para>The type of class returned must implement <see
		///    cref="IApplicationHost" />.</para>
		///    <para>This type is used internally to create a <see
		///    cref="IApplicationHost" /> in a specified <see
		///    cref="AppDomain" /> via <see
		///    cref="Web.Hosting.ApplicationHost.CreateApplicationHost"
		///    />.</para>
		/// </remarks>
		public abstract Type GetApplicationHostType ();
		
		/// <summary>
		///    Creates a request broker for managing requests.
		/// </summary>
		/// <returns>
		///    A <see cref="IRequestBroker" /> containing a request
		///    broker for managing requests.
		/// </returns>
		/// <remarks>
		///    Each application host receives its own request broker.
		/// </remarks>
		public abstract IRequestBroker CreateRequestBroker ();

		/// <summary>
		///    Disposes of the the resources contained in the current
		///    instance.
		/// </summary>
		/// <remarks>
		///    Implemented for <see cref="IDisposable" />.
		/// </remarks>
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		/// <summary>
		///    Disposes of the the resources contained in the current
		///    instance.
		/// </summary>
		/// <param name="disposing">
		///    A <see cref="bool" /> indicating whether or not the
		///    current instance is disposing. If <see langword="false"
		///    />, the method was called by the class and not the
		///    garbage collector.
		/// </param>
		protected virtual void Dispose (bool disposing)
		{
		}
	}	
}
