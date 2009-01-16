//
// RequestReader.cs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
// (C) Copyright 2004-2008 Novell, Inc. (http://www.novell.com)
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
using System.Collections;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Mono.Security.X509;
using Mono.Security.X509.Extensions;

namespace Mono.WebServer
{
	public class RequestReader
	{
		ModMonoRequest request;

		public ModMonoRequest Request {
			get { return request; }
		}
		
		public RequestReader (Socket client)
		{
			this.request = new ModMonoRequest (client);
		}
		
		public string GetUriPath ()
		{
			string path = request.GetUri ();

			int dot = path.LastIndexOf ('.');
			int slash = (dot != -1) ? path.IndexOf ('/', dot) : 0;
			if (dot > 0 && slash > 0)
				path = path.Substring (0, slash);

			return path;
		}

		public string GetPhysicalPath ()
		{
			return request.GetPhysicalPath ();
		}

		public void Decline ()
		{
			request.Decline ();
		}

		public void NotFound ()
		{
			request.NotFound ();
		}

		public bool ShuttingDown {
			get { return request.ShuttingDown; }
		}
	}
}
