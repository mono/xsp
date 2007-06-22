//
// Mono.WebServer.IApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Documentation:
//	Brian Nickel
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
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
using System.Web;

namespace Mono.WebServer
{
	/// <summary>
	///    This interface is used for classes that serve as application
	///    hosts.
	/// </summary>
	/// <remarks>
	///    An application, as created through a <see
	///    cref="ApplicationServer" />, exists in its own <see
	///    cref="AppDomain" />.
	/// </remarks>
	public interface IApplicationHost
	{
		/// <summary>
		///    Gets the physical path of the hosted application.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the physical path of
		///    the hosted application.
		/// </value>
		string Path { get; }
		
		/// <summary>
		///    Gets the virtual path of the hosted application.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the virtual path of
		///    the hosted application.
		/// </value>
		string VPath { get; }	
		
		/// <summary>
		///    Gets the app-domain the application host exists in.
		/// </summary>
		/// <value>
		///    A <see cref="AppDomain" /> object containing the
		///    app-domain the application host exists in.
		/// </value>
		AppDomain Domain { get; }	
		
		/// <summary>
		///    Gets and sets the request broker that manages the hosted
		///    requests.
		/// </summary>
		/// <value>
		///    A <see cref="IRequestBroker" /> object containing the
		///    request broker that manages the hosted requests.
		/// </value>
		IRequestBroker RequestBroker { get; set; }
		
		/// <summary>
		///    Gets the application server that created the application
		///    host.
		/// </summary>
		/// <value>
		///    A <see cref="ApplicationServer" /> object containing the
		///    application server that created the application host.
		/// </value>
		ApplicationServer Server { get; set; }
		
		/// <summary>
		///    Unloads the application host.
		/// </summary>
		void Unload ();
	}
	
	/// <summary>
	///    This interface is used for classes that manage requests.
	/// </summary>
	/// <remarks>
	///    A request broker serves as an intermediary between <see
	///    cref="Worker" /> and <see cref="MonoWorkerRequest" /> to handle
	///    the interaction between app-domains. In addition it should
	///    inherit <see cref="MarshalByRefObject" />.
	/// </remarks>
	public interface IRequestBroker
	{
	}
}

