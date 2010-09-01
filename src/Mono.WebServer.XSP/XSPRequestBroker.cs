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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Protocol.Tls;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Mono.WebServer
{	
	//
	// XSPRequestBroker: The request broker for XSP. Provides some
	// additional methods to the BaseRequestBroker from which inherits.
	//
	[Obsolete ("This class should not be used. It will be removed from Mono.WebServer.dll")]
	public class XSPRequestBroker: BaseRequestBroker
	{
		public int GetReuseCount (int requestId)
		{
			XSPWorker worker = (XSPWorker) GetWorker (requestId);
			if (worker == null)
				return 0;
			
			return worker.GetRemainingReuses ();
		}

		public void Close (int requestId, bool keepAlive)
		{
			XSPWorker worker = (XSPWorker) GetWorker (requestId);
			if (worker != null) {
				try {
					worker.Close (keepAlive);
				} catch {}
			}
		}
	}
}
