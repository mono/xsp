//
// ApplicationHost.cs: Hosts ASP.NET applications in their own AppDomain.
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
using Mono.FastCgi;
using Mono.WebServer;
using System.Web;
using System.IO;
using System.Text;

namespace Mono.WebServer.FastCgi
{
	public class ApplicationHost : BaseApplicationHost
	{
		public ApplicationHost ()
		{
		}
		
		public void ProcessRequest (Responder responder)
		{
			WorkerRequest worker = new WorkerRequest (responder,
				this);
			
			string path = responder.Path;
			if (path [path.Length - 1] != '/' && Directory.Exists (
				worker.MapPath (path))) {
				Redirect (worker, path + '/');
				return;
			}
			
			ProcessRequest (worker);
		}
		
		private static string content301 =
			"<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
			"<html><head>\n<title>301 Moved Permanently</title>\n</head><body>\n" +
			"<h1>Moved Permanently</h1>\n" +
			"<p>The document has moved to <a href='http://{0}{1}'>http://{0}{1}</a>.</p>\n" +
			"</body></html>\n";
		
		private static void Redirect (MonoWorkerRequest wr, string location)
		{
			string host = wr.GetKnownRequestHeader (HttpWorkerRequest.HeaderHost);
			wr.SendStatus (301, "Moved Permanently");
			wr.SendUnknownResponseHeader ("Connection", "close");
			wr.SendUnknownResponseHeader ("Date", DateTime.Now.ToUniversalTime ().ToString ("r"));
			wr.SendUnknownResponseHeader ("Location", String.Format ("http://{0}{1}", host, location));
			Encoding enc = Encoding.ASCII;
			wr.SendUnknownResponseHeader ("Content-Type", "text/html; charset=" + enc.WebName);
			string content = String.Format (content301, host, location);
			byte [] contentBytes = enc.GetBytes (content);
			wr.SendUnknownResponseHeader ("Content-Length", contentBytes.Length.ToString ());
			wr.SendResponseFromMemory (contentBytes, contentBytes.Length);
			wr.FlushResponse (true);
			wr.CloseConnection ();
		}
	}
}
