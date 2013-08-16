//
// Mono.WebServer.ModMonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using Mono.Unix;
using Mono.WebServer.Apache;
using Mono.WebServer.Log;

namespace Mono.WebServer
{
	//
	// ModMonoWebSource: Provides methods to get objects and types specific
	// to mod_mono.
	//
	public class ModMonoWebSource: WebSource
	{
		string filename;
		bool file_bound;
		Stream locker;
		readonly string lockfile;

		protected ModMonoWebSource (string lockfile)
		{
			this.lockfile = lockfile;
			try {
				locker = File.OpenWrite (lockfile);
			} catch {
				// Silently exit. Many people confused about this harmless message.
				//Logger.Write (LogLevel.Error, "Another mod-mono-server with the same arguments is already running.");
				Environment.Exit (1);
			}
		}

		public ModMonoWebSource (string filename, string lockfile)
			: this (lockfile)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");

			this.filename = filename;
		}

		public virtual bool GracefulShutdown ()
		{
			EndPoint ep = new UnixEndPoint (filename);
			var sock = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			try {
				sock.Connect (ep);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error, "Cannot connect to {0}: {1}", filename, e.Message);
				return false;
			}

			return SendShutdownCommandAndClose (sock);
		}

		protected bool SendShutdownCommandAndClose (Socket sock)
		{
			var b = new byte [] {0};
			bool result = true;
			try {
				int sent = sock.Send (b);
				if (sent != b.Length)
					throw new IOException ("Blocking send did not send entire buffer");
			} catch {
				result = false;
			}

			sock.Close ();
			return result;
		}

		public override Socket CreateSocket ()
		{
			if (filename == null)
				throw new InvalidOperationException ("filename not set");

			EndPoint ep = new UnixEndPoint (filename);
			if (File.Exists (filename)) {
				var conn = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				try {
					conn.Connect (ep);
					conn.Close ();
					throw new InvalidOperationException ("There's already a server listening on " + filename);
				} catch (SocketException) {
				}
				File.Delete (filename);
			}

			var listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (ep);
			file_bound = true;
			return listen_socket;
		}
 
		public override Worker CreateWorker (Socket client, ApplicationServer server)
		{
			return new ModMonoWorker (client, server);
		}
		
		public override Type GetApplicationHostType ()
		{
			return typeof (ModMonoApplicationHost);
		}
		
		public override IRequestBroker CreateRequestBroker ()
		{
			return new ModMonoRequestBroker ();
		}

		protected override void Dispose (bool expl)
		{
			if (filename != null) {
				string f = filename;
				filename = null;
				if (file_bound)
					File.Delete (f);
			}

			if (locker != null) {
				Stream l = locker;
				locker = null;
				l.Close ();
				File.Delete (lockfile);
			}
		}
		
		~ModMonoWebSource ()
		{
			Dispose (false);
		}
	}
}
