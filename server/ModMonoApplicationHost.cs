//
// Mono.ASPNET.ModMonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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
using Mono.Posix;

namespace Mono.ASPNET
{
	//
	// ModMonoWebSource: Provides methods to get objects and types specific
	// to mod_mono.
	//
	public class ModMonoWebSource: IWebSource, IDisposable
	{
		string filename;
		bool file_bound;
		Stream locker;
		string lockfile;

		protected ModMonoWebSource (string lockfile)
		{
			this.lockfile = lockfile;
			try {
				locker = File.OpenWrite (lockfile);
			} catch {
				Console.Error.WriteLine ("Another mod-mono-server with the same arguments is already running.");
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
			Socket sock = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			try {
				sock.Connect (ep);
			} catch (Exception e) {
				Console.Error.WriteLine ("Cannot connect to {0}: {1}", filename, e.Message);
				return false;
			}

			return SendShutdownCommandAndClose (sock);
		}

		protected bool SendShutdownCommandAndClose (Socket sock)
		{
			byte [] b = new byte [] {0};
			bool result = true;
			try {
				sock.Send (b);
			} catch {
				result = false;
			}

			sock.Close ();
			return result;
		}

		public virtual Socket CreateSocket ()
		{
			if (filename == null)
				throw new InvalidOperationException ("filename not set");

			EndPoint ep = new UnixEndPoint (filename);
			if (File.Exists (filename)) {
				Socket conn = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				try {
					conn.Connect (ep);
					conn.Close ();
					throw new InvalidOperationException ("There's already a server listening on " + filename);
				} catch (SocketException se) {
				}
				File.Delete (filename);
			}

			Socket listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (ep);
			file_bound = true;
			return listen_socket;
		}
 
		public IWorker CreateWorker (Socket client, ApplicationServer server)
		{
			return new ModMonoWorker (client, server);
		}
		
		public Type GetApplicationHostType ()
		{
			return typeof (ModMonoApplicationHost);
		}
		
		public IRequestBroker CreateRequestBroker ()
		{
			return new ModMonoRequestBroker ();
		}

		public void Dispose ()
		{
			Dispose (true);
		}

		protected virtual void Dispose (bool expl)
		{
			if (filename != null) {
				string f = filename;
				filename = null;
				if (file_bound)
					File.Delete (f);

				if (locker != null) {
					locker.Close ();
					File.Delete (lockfile);
				}
			}
		}
		
		~ModMonoWebSource ()
		{
			Dispose (false);
		}
	}
	
	//
	// ModMonoRequestBroker: The request broker for mod_mono. Provides some
	// additional methods to the BaseRequestBroker from which inherits.
	//
	public class ModMonoRequestBroker: BaseRequestBroker
	{
		public string GetServerVariable (int requestId, string name)
		{
			return ((ModMonoWorker)GetWorker (requestId)).GetServerVariable (name);
		}

		public void SetStatusCodeLine (int requestId, int code, string status)
		{
			((ModMonoWorker)GetWorker (requestId)).SetStatusCodeLine (code, status);
		}
		
		public void SetResponseHeader (int requestId, string name, string value)
		{
			((ModMonoWorker)GetWorker (requestId)).SetResponseHeader (name, value);
		}
	}

	//
	// ModMonoApplicationHost: The application host for mod_mono.
	//
	public class ModMonoApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (int reqId, string verb, string queryString, string path,
					string protocol, string localAddress, int serverPort, string remoteAddress,
					int remotePort, string remoteName, string [] headers, string [] headerValues)
		{
			ModMonoRequestBroker broker = (ModMonoRequestBroker) RequestBroker;
			ModMonoWorkerRequest mwr = new ModMonoWorkerRequest (reqId, broker, this, verb, path, queryString,
									protocol, localAddress, serverPort, remoteAddress,
									remotePort, remoteName, headers, headerValues);

			ProcessRequest (mwr);
		}
	}
	
	//
	// ModMonoWorker: The worker that do the initial processing of mod_mono
	// requests.
	//
	internal class ModMonoWorker: IWorker
	{
		ApplicationServer server;
		public LingeringNetworkStream Stream;
		ModMonoRequest modRequest;

		public ModMonoWorker (Socket client, ApplicationServer server)
		{
			Stream = new LingeringNetworkStream (client, true);
			Stream.EnableLingering = false;
			this.server = server;
		}

		public void Run (object state)
		{
			int requestId = -1;
			ModMonoRequestBroker broker = null;
			
			try {
				RequestReader rr = new RequestReader (Stream);
				if (rr.ShuttingDown)
					server.Stop ();

				string vhost = rr.Request.GetRequestHeader ("Host");
				int port = -1;
				if (vhost != null) {
					int colon = vhost.IndexOf (':');
					if (colon != -1) {
						port = Int32.Parse (vhost.Substring (colon + 1));
						vhost = vhost.Substring (0, colon);
					} else {
						port = 80;
					}
				}
				
				VPathToHost vapp = server.GetApplicationForPath (vhost, port, rr.GetUriPath (), false);
				if (vapp == null) {
					rr.NotFound (); // No app to handle the request
					return;
				}

				ModMonoApplicationHost host = (ModMonoApplicationHost) vapp.AppHost;
				if (host == null) {
					rr.Decline ();
					return;
				}
				modRequest = rr.Request;
				
				broker = (ModMonoRequestBroker) vapp.RequestBroker; 
				requestId = broker.RegisterRequest (this);
				
				host.ProcessRequest (requestId, 
									modRequest.GetHttpVerbName(), 
									modRequest.GetQueryString(), 
									modRequest.GetUri(), 
									modRequest.GetProtocol(), 
									modRequest.GetLocalAddress(), 
									modRequest.GetServerPort(), 
									modRequest.GetRemoteAddress(), 
									modRequest.GetRemotePort(), 
									modRequest.GetRemoteName(), 
									modRequest.GetAllHeaders(),
									modRequest.GetAllHeaderValues());
				
			} catch (Exception e) {
				Console.WriteLine ("In ModMonoWorker.Run: {0}", e.Message);
				try {
					// Closing is enough for mod_mono. the module will return a 50x
					Stream.Close ();
				} catch {}

				if (broker != null && requestId != -1)
					broker.UnregisterRequest (requestId);
			}
		}

		public int Read (byte[] buffer, int position, int size)
		{
			return modRequest.GetClientBlock (buffer, position, size);
		}
		
		public void Write (byte[] buffer, int position, int size)
		{
			modRequest.SendResponseFromMemory (buffer, position, size);
		}
		
		public void Close ()
		{
			modRequest.Close ();
		}
		
		public void Flush ()
		{
			modRequest.Flush ();
		}
		
		public string GetServerVariable (string name)
		{
			return modRequest.GetServerVariable (name);
		}
		
		public void SetStatusCodeLine (int code, string status)
		{
			modRequest.SetStatusCodeLine (code, status);
		}
		
		public void SetResponseHeader (string name, string value)
		{
			modRequest.SetResponseHeader (name, value);
		}
	}	
}
