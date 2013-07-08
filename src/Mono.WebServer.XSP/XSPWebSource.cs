//
// Mono.WebServer.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004-2007 Novell, Inc. (http://www.novell.com)
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
using System.Net;
using System.Net.Sockets;
using Mono.Security.Protocol.Tls;
using Mono.WebServer.XSP;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Mono.WebServer {
	//
	// XSPWebSource: Provides methods to get objects and types specific
	// to XSP.
	//
	public class XSPWebSource: WebSource
	{
		IPEndPoint bindAddress;
		readonly bool secureConnection;
		readonly SecurityProtocolType securityProtocol;
		readonly X509Certificate cert;
		readonly PrivateKeySelectionCallback keyCB;
		readonly bool allowClientCert;
		readonly bool requireClientCert;

		public XSPWebSource(IPAddress address, int port, SecurityProtocolType securityProtocol,
				    X509Certificate cert, PrivateKeySelectionCallback keyCB, 
				    bool allowClientCert, bool requireClientCert, bool single_app)
		{			
			secureConnection = (cert != null && keyCB != null);
			bindAddress = new IPEndPoint (address, port);
			this.securityProtocol = securityProtocol;
			this.cert = cert;
			this.keyCB = keyCB;
			this.allowClientCert = allowClientCert;
			this.requireClientCert = requireClientCert;
		}

		public XSPWebSource (IPAddress address, int port, bool single_app)
		{
			SetListenAddress (address, port);
		}

		public XSPWebSource (IPAddress address, int port) : this (address, port, false)
		{
		}

		public void SetListenAddress (int port)
		{
			SetListenAddress (IPAddress.Any, port);
		}

		public void SetListenAddress (IPAddress address, int port) 
		{
			SetListenAddress (new IPEndPoint (address, port));
		}
		
		public void SetListenAddress (IPEndPoint bindAddress)
		{
			if (bindAddress == null)
				throw new ArgumentNullException ("bindAddress");

			this.bindAddress = bindAddress;
		}
		
		public override Socket CreateSocket ()
		{
			var listen_socket = new Socket (bindAddress.AddressFamily,
				SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (bindAddress);
			return listen_socket;
		} 

		public override Worker CreateWorker (Socket client, ApplicationServer server)
		{
			return new XSPWorker (client, client.LocalEndPoint, server,
				secureConnection, securityProtocol, cert, keyCB, allowClientCert, requireClientCert);
		}
		
		public override Type GetApplicationHostType ()
		{
			return typeof (XSPApplicationHost);
		}
		
		public override IRequestBroker CreateRequestBroker ()
		{
			return new XSPRequestBroker ();
		}
	}
}
