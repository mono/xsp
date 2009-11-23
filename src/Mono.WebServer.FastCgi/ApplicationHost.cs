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
		public ApplicationHost ()
		{
		}
		
		public void ProcessRequest (Responder responder)
		{
			WorkerRequest worker = new WorkerRequest (responder,
				this);
			
			string path = responder.Path;
			int len = path != null ? path.Length : 0;
			if (len > 0 && path [len - 1] != '/' && VirtualDirectoryExists (path, worker)) {
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
		
		/// <summary>
		///    Splits a virtual URI into its virtual path and virtual 
		///    path-info parts as identified by the web-application 
		///    host in the current <see cref="AppDomain" />.
		/// </summary>
		/// <param name="verb">
		///    The HTTP verb of the request.
		/// </param>
		/// <param name="uri">
		///    the virtual URI, including any path-info, which need to be split.
		/// </param>
		/// <param name="realUri">
		///    Returns the path part of the URI.
		/// </param>
		/// <param name="pathInfo">
		///    Returns the trailing path-info part of the URI, if any.
		/// </param>
		/// <remarks>
		///    This method wraps the <see 
		///    cref="Mono.WebServer.Paths.GetPathsFromUri" /> 
		///    method so that it can easily be called 
		///    from a remote <see cref="AppDomain" />.
		/// </remarks>
		public void GetPathsFromUri (string verb, string uri, out string realUri, out string pathInfo)
		{
			Paths.GetPathsFromUri (this, verb, uri, out realUri, out pathInfo);
		}
		
		/// <summary>
		///    Maps the specified virtual path to a physical path on 
		///    the server as defined in the <see cref="AppDomain" /> 
		///    of the current <see cref="ApplicationHost" />.
		/// </summary>
		/// <param name="virtualPath">
		///    The virtual path to be mapped.
		/// </param>
		/// <returns>
		///    The physical path of the specified virtual path.
		/// </returns>
		/// <remarks>
		///    As a method that is available on this <see 
		///    cref="ApplicationHost" />, which derives 
		///    from <see cref="MarshalByRefObject" />, it 
		///    enables path mapping to be queried from a 
		///    remote <see cref="AppDomain" />.
		/// </remarks>
		public string MapPath (string virtualPath)
		{
			string physPath;

#if NET_2_0
			physPath = HostingEnvironment.MapPath ((virtualPath == null || virtualPath.Length == 0 || virtualPath.TrimStart().Length == 0) ? "/" : virtualPath);
			if (physPath != null && physPath.Length != 0)
				return physPath;
#endif

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

			if (virtualPath == null || virtualPath.Length == 0 || virtualPath == this.VPath) {
				if (IOPath.DirectorySeparatorChar != '/')
					return this.Path.Replace ('/', IOPath.DirectorySeparatorChar);
				else
					return this.Path;
			}

			physPath = virtualPath;
			if (physPath[0] == '~' && physPath.Length > 2 && physPath[1] == '/')
				physPath = physPath.Substring (1);

			int len = this.VPath.Length;
			if (physPath.StartsWith (this.VPath) && (physPath.Length == len || physPath[len] == '/'))
				physPath = physPath.Substring (len + 1);

			int i = 0;
			len = physPath.Length;
			while (i < len && physPath [i] == '/')
				i++;
			
			if (i < len)
				physPath = physPath.Substring (i);
			else
				return this.Path;
			
			if (IOPath.DirectorySeparatorChar != '/')
				physPath = physPath.Replace ('/', IOPath.DirectorySeparatorChar);

			return IOPath.Combine (this.Path, physPath);
		}
		
		/// <summary>
		///    Determines if a virtual path specifies a file on 
		///    the server as defined in the <see cref="AppDomain" /> 
		///    of the current <see cref="ApplicationHost" />.
		/// </summary>
		/// <param name="virtualPath">
		///    The virtual path to be checked.
		/// </param>
		/// <returns>
		///    True if the virtual path is that of a file, otherwise false.
		/// </returns>
		/// <remarks>
		///    As a method that is available on this <see 
		///    cref="ApplicationHost" />, which derives 
		///    from <see cref="MarshalByRefObject" />, it 
		///    enables virtual file checks to be performed 
		///    from a remote <see cref="AppDomain" />.
		/// </remarks>
		public bool VirtualFileExists (string virtualPath)
		{
#if NET_2_0
			VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
			// TODO: Remove the second condition of the "if" statement (it is only a workaround) involving DefaultVirtualPathProvider as soon as Mono's DefaultVirtualPathProvider.FileExists method works properly (i.e., the indirectly-called HostingEnvironment.MapPath method should not require an HttpContext.Current.Request object to do its work; also see the comment in the ApplicationHost.MapPath method above)
			if (vpp != null && !vpp.GetType().FullName.Equals("System.Web.Hosting.DefaultVirtualPathProvider", StringComparison.Ordinal))
				return vpp.FileExists (virtualPath);
#endif
			return File.Exists (MapPath (virtualPath));
		}
		
		/// <summary>
		///    Determines if a virtual path specifies a directory on 
		///    the server as defined in the <see cref="AppDomain" /> 
		///    of the current <see cref="ApplicationHost" />, while 
		///    possibly using the specified <see cref="WorkerRequest" /> 
		///    to help with path mapping if needed.
		/// </summary>
		/// <param name="virtualPath">
		///    The virtual path to be checked.
		/// </param>
		/// <param name="worker">
		///    The worker request that should be used to perform 
		///    the mapping of the virtual path to the physical 
		///    path if no virtual-path provider is registered 
		///    in the current <see cref="AppDomain" />.
		/// </param>
		/// <returns>
		///    True if the virtual path is that of a directory, otherwise false.
		/// </returns>
		private bool VirtualDirectoryExists (string virtualPath, WorkerRequest worker)
		{
#if NET_2_0
			VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
			// TODO: Remove the second condition of the "if" statement (it is only a workaround) involving DefaultVirtualPathProvider as soon as Mono's DefaultVirtualPathProvider.DirectoryExists method works properly (i.e., the indirectly-called HostingEnvironment.MapPath method should not require an HttpContext.Current.Request object to do its work; also see the comment in the ApplicationHost.MapPath method above)
			if (vpp != null && !vpp.GetType().FullName.Equals("System.Web.Hosting.DefaultVirtualPathProvider", StringComparison.Ordinal))
				return vpp.DirectoryExists (virtualPath);
#endif
			string physicalPath = (worker != null) ? worker.MapPath (virtualPath) : MapPath (virtualPath);
			return Directory.Exists (physicalPath);
		}
	}
}
