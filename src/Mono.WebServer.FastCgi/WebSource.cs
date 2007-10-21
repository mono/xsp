//
// WebSource.cs: Provides a shell implementation of Mono.WebServer.WebSource
// for ApplicationServer to get the IApplicationHost type from.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

namespace Mono.WebServer.FastCgi {
	
	// FIXME: This class could be removed if
	// Mono.WebServer.ApplicationServer is broken into two classes:
		// * ApplicationManager to handle application hosts, and
		// * ApplicationServer, a subclass of ApplicationManager that
		//   adds on server support.
	
	public class WebSource : Mono.WebServer.WebSource {
		public WebSource ()
		{
		}
		
		public override IRequestBroker CreateRequestBroker()
		{
			return null;
		}
		
		public override System.Type GetApplicationHostType()
		{
			return typeof(ApplicationHost);
		}
		
		public override Worker CreateWorker(System.Net.Sockets.Socket socket, ApplicationServer server)
		{
			return null;
		}
		
		public override System.Net.Sockets.Socket CreateSocket()
		{
			return null;
		}
	}
}
