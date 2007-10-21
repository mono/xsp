using System;
using System.Globalization;

namespace Mono.FastCgi
{
	internal class UnixSocket : StandardSocket, IDisposable
	{
		string path = null;
		
		protected UnixSocket (Mono.Unix.UnixEndPoint localEndPoint)
			: base (System.Net.Sockets.AddressFamily.Unix,
			        System.Net.Sockets.SocketType.Stream,
			        System.Net.Sockets.ProtocolType.IP,
			        localEndPoint)
		{
		}
		
		public UnixSocket (string path) : this (CreateEndPoint (path))
		{
			this.path = path;
		}
		
		
		protected static Mono.Unix.UnixEndPoint CreateEndPoint (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			
			Mono.Unix.UnixEndPoint ep = new Mono.Unix.UnixEndPoint (
				path);
			
			if (System.IO.File.Exists (path)) {
				System.Net.Sockets.Socket conn =
					new System.Net.Sockets.Socket (
						System.Net.Sockets.AddressFamily.Unix,
						System.Net.Sockets.SocketType.Stream,
						System.Net.Sockets.ProtocolType.IP);
				
				try {
					conn.Connect (ep);
					conn.Close ();
					throw new InvalidOperationException (
						string.Format (CultureInfo.CurrentCulture,
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
                		System.IO.File.Delete (f);
                	}
		}
		
                ~UnixSocket ()
                {
                	Dispose ();
                }
	}
}