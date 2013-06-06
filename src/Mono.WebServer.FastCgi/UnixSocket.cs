//
// UnixSocket.cs: Provides a wrapper around a unix domain socket file.
//
// Authors:
//   Brian Nickel (brian.nickel@gmail.com)
//   Andres G. Aragoneses (andres@7digital.com)
//
// Copyright (C) 2007 Brian Nickel
// Copyright (C) 2013 7digital Media Ltd (http://www.7digital.com)
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
using Mono.Unix;
using System.Globalization;

namespace Mono.FastCgi
{
	internal class UnixSocket : StandardSocket, IDisposable
	{
		string path;
		readonly long inode;
		
		protected UnixSocket (UnixEndPoint localEndPoint)
			: base (System.Net.Sockets.AddressFamily.Unix,
			        System.Net.Sockets.SocketType.Stream,
			        System.Net.Sockets.ProtocolType.IP,
			        localEndPoint)
		{
		}
		
		public UnixSocket (string path) : this (CreateEndPoint (path))
		{
			this.path = path;
			inode = new UnixFileInfo (path).Inode;
		}
		
		
		protected static UnixEndPoint CreateEndPoint (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			
			var ep = new UnixEndPoint (path);
			
			if (System.IO.File.Exists (path)) {
				var conn = new System.Net.Sockets.Socket (
					System.Net.Sockets.AddressFamily.Unix,
					System.Net.Sockets.SocketType.Stream,
					System.Net.Sockets.ProtocolType.IP);
				
				try {
					conn.Connect (ep);
					conn.Close ();
					throw new InvalidOperationException (
						String.Format (CultureInfo.CurrentCulture,
							Strings.UnixSocket_AlreadyExists,
							path));
				} catch (System.Net.Sockets.SocketException) {
				}
				
				System.IO.File.Delete (path);
			}
			
			return ep;
		}
		
		public void Dispose ()
		{
                	if (path != null) {
                		string f = path;
                		path = null;

				if (System.IO.File.Exists (f) && inode == new UnixFileInfo (f).Inode) {
					System.IO.File.Delete (f);
				}
                	}
		}
		
                ~UnixSocket ()
                {
                	Dispose ();
                }
	}
}

