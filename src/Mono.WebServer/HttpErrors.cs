//
// HttpErrors.cs
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (c) Copyright 2002-2007 Novell, Inc
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
using System.Text;

namespace Mono.WebServer
{
	public class HttpErrors
	{
		static readonly byte [] error500;
		static readonly byte [] badRequest;

		static HttpErrors ()
		{
			const string s = "HTTP/1.0 500 Server error\r\n" +
				"Connection: close\r\n\r\n" +
				"<html><head><title>500 Server Error</title><body><h1>Server error</h1>\r\n" +
				"Your client sent a request that was not understood by this server.\r\n" +
				"</body></html>\r\n";
			error500 = Encoding.ASCII.GetBytes (s);

			const string br = "HTTP/1.0 400 Bad Request\r\n" + 
				"Connection: close\r\n\r\n" +
				"<html><head><title>400 Bad Request</title></head>" +
				"<body><h1>Bad Request</h1>The request was not understood" +
				"<p></body></html>";

			badRequest = Encoding.ASCII.GetBytes (br);
		}

		public static byte [] NotFound (string uri)
		{
			string s = String.Format ("HTTP/1.0 404 Not Found\r\n" + 
						  "Connection: close\r\n\r\n" +
						  "<html><head><title>404 Not Found</title></head>\r\n" +
						  "<body><h1>Not Found</h1>The requested URL {0} was not found on this " +
						  "server.<p>\r\n</body></html>\r\n", uri);

			return Encoding.ASCII.GetBytes (s);
		}

		public static byte [] BadRequest ()
		{
			return badRequest;
		}

		public static byte [] ServerError ()
		{
			return error500;
		}
	}
}
