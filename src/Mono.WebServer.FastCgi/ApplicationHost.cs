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
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

using IOPath = System.IO.Path;

namespace Mono.WebServer.FastCgi
{
	public class ApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (Responder responder)
		{
			var worker = new WorkerRequest (responder,
				this);
			
			string path = responder.Path;
			if (!String.IsNullOrEmpty(path) && path [path.Length - 1] != '/' 
				&& VirtualDirectoryExists (path, worker)) {
				Redirect (worker, path + '/');
				return;
			}
			
			ProcessRequest (worker);
		}
		
		const string CONTENT301 =
			"<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
			"<html><head>\n<title>301 Moved Permanently</title>\n</head><body>\n" +
			"<h1>Moved Permanently</h1>\n" +
			"<p>The document has moved to <a href='http://{0}{1}'>http://{0}{1}</a>.</p>\n" +
			"</body></html>\n";
		
		static void Redirect (HttpWorkerRequest wr, string location)
		{
			string host = wr.GetKnownRequestHeader (HttpWorkerRequest.HeaderHost);
			wr.SendStatus (301, "Moved Permanently");
			wr.SendUnknownResponseHeader ("Connection", "close");
			wr.SendUnknownResponseHeader ("Date", DateTime.Now.ToUniversalTime ().ToString ("r"));
			wr.SendUnknownResponseHeader ("Location", String.Format ("http://{0}{1}", host, location));
			Encoding enc = Encoding.ASCII;
			wr.SendUnknownResponseHeader ("Content-Type", "text/html; charset=" + enc.WebName);
			string content = String.Format (CONTENT301, host, location);
			byte [] contentBytes = enc.GetBytes (content);
			wr.SendUnknownResponseHeader ("Content-Length", contentBytes.Length.ToString ());
			wr.SendResponseFromMemory (contentBytes, contentBytes.Length);
			wr.FlushResponse (true);
			wr.CloseConnection ();
		}
		
		public void GetPathsFromUri (string verb, string uri, out string realUri, out string pathInfo)
		{
			Paths.GetPathsFromUri (this, verb, uri, out realUri, out pathInfo);
		}
		
		public string MapPath (string virtualPath)
		{
			string physPath = HostingEnvironment.MapPath ((String.IsNullOrEmpty(virtualPath) || virtualPath.TrimStart().Length == 0) ? "/" : virtualPath);

			if (!String.IsNullOrEmpty(physPath))
				return physPath;

			// For old .NET 1.x, and as a fallback mechanism until Mono's 
			// HostingEnvironment.MapPath method can perform the mapping 
			// without requiring an HttpContext.Current.Request object
			// (the MS one can do it), just map the path somewhat similar 
			// to the Mono.WebServer.MonoWorkerRequest.MapPath method (but 
			// unfortunately without the customizable event mechanism)...
			// TODO: Remove the logic below for NET_2_0 as soon as Mono's
			// DefaultVirtualPathProvider.MapPath method works properly (and then also
			// remove the workarounds in ApplicationHost.VirtualFileExists and
			// ApplicationHost.VirtualDirectoryExists)

			if (String.IsNullOrEmpty(virtualPath) || virtualPath == VPath) {
				if (IOPath.DirectorySeparatorChar != '/')
					return Path.Replace ('/', IOPath.DirectorySeparatorChar);
				return Path;
			}

			physPath = virtualPath;
			if (physPath[0] == '~' && physPath.Length > 2 && physPath[1] == '/')
				physPath = physPath.Substring (1);

			int len = VPath.Length;
			if (physPath.StartsWith (VPath) && (physPath.Length == len || physPath[len] == '/'))
				physPath = physPath.Substring (len + 1);

			int i = 0;
			len = physPath.Length;
			while (i < len && physPath [i] == '/')
				i++;
			
			if (i < len)
				physPath = physPath.Substring (i);
			else
				return Path;
			
			if (IOPath.DirectorySeparatorChar != '/')
				physPath = physPath.Replace ('/', IOPath.DirectorySeparatorChar);

			return IOPath.Combine (Path, physPath);
		}
		
		public bool VirtualFileExists (string virtualPath)
		{
			VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
			// TODO: Remove the second condition of the "if" statement (it is only a workaround) involving DefaultVirtualPathProvider as soon as Mono's DefaultVirtualPathProvider.FileExists method works properly (i.e., the indirectly-called HostingEnvironment.MapPath method should not require an HttpContext.Current.Request object to do its work; also see the comment in the ApplicationHost.MapPath method above)
			if (vpp != null && !vpp.GetType().FullName.Equals("System.Web.Hosting.DefaultVirtualPathProvider", StringComparison.Ordinal))
				return vpp.FileExists (virtualPath);

			return File.Exists (MapPath (virtualPath));
		}
		
		bool VirtualDirectoryExists (string virtualPath, HttpWorkerRequest worker)
		{
			VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
			// TODO: Remove the second condition of the "if" statement (it is only a workaround) involving DefaultVirtualPathProvider as soon as Mono's DefaultVirtualPathProvider.DirectoryExists method works properly (i.e., the indirectly-called HostingEnvironment.MapPath method should not require an HttpContext.Current.Request object to do its work; also see the comment in the ApplicationHost.MapPath method above)
			if (vpp != null && !vpp.GetType().FullName.Equals("System.Web.Hosting.DefaultVirtualPathProvider", StringComparison.Ordinal))
				return vpp.DirectoryExists (virtualPath);

			string physicalPath = (worker != null) ? worker.MapPath (virtualPath) : MapPath (virtualPath);
			return Directory.Exists (physicalPath);
		}
	}
}
