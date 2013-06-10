//
// Paths.cs
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
using System.Web;
using System.Web.Hosting;

namespace Mono.WebServer
{
	public static class Paths
	{
		public static void GetPathsFromUri (IApplicationHost appHost, string verb, string uri, out string realUri, out string pathInfo)
		{
			// There's a hidden missing feature here... :)
			realUri = uri; pathInfo = String.Empty;
			string vpath = HttpRuntime.AppDomainAppVirtualPath;
			int vpathLen = vpath.Length;
			
			if (vpath [vpathLen - 1] != '/')
				vpath += '/';

			if (vpathLen > uri.Length)
				return;

			uri = uri.Substring (vpathLen);
			while (uri.Length > 0 && uri [0] == '/')
				uri = uri.Substring (1);

			int lastSlash = uri.Length;

			for (int dot = uri.LastIndexOf ('.'); dot > 0; dot = uri.LastIndexOf ('.', dot - 1)) {
				int slash = uri.IndexOf ('/', dot);
				
				if (slash == -1)
					slash = lastSlash;
				
				string partial = uri.Substring (0, slash);
				lastSlash = slash;

				if (!VirtualPathExists (appHost, verb, partial))
					continue;
				
				realUri = vpath + uri.Substring (0, slash);
				pathInfo = uri.Substring (slash);
				break;
			}
		}

		static bool VirtualPathExists (IApplicationHost appHost, string verb, string uri)
		{
			if (appHost.IsHttpHandler (verb, uri))
				return true;

			VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
			return vpp != null && vpp.FileExists ("/" + uri);
		}
	}
}
