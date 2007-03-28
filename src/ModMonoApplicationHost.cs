//
// Mono.WebServer.ModMonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004,2005 Novell, Inc. (http://www.novell.com)
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
using System.Security.Cryptography.X509Certificates;
using Mono.Unix;

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
		string lockfile;

		protected ModMonoWebSource (string lockfile)
		{
			this.lockfile = lockfile;
			try {
				locker = File.OpenWrite (lockfile);
			} catch {
				// Silently exit. Many people confused about this harmless message.
				//Console.Error.WriteLine ("Another mod-mono-server with the same arguments is already running.");
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

		public override Socket CreateSocket ()
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
				} catch (SocketException) {
				}
				File.Delete (filename);
			}

			Socket listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
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

		public void SendFile (int requestId, string filename)
		{
			((ModMonoWorker)GetWorker (requestId)).SendFile (filename);
		}
	}

	//
	// ModMonoApplicationHost: The application host for mod_mono.
	//
	public class ModMonoApplicationHost : BaseApplicationHost
	{
		private byte[] FromPEM (string pem) 
		{
			int start = pem.IndexOf ("-----BEGIN CERTIFICATE-----");
			if (start < 0)
				return null;

			start += 27; // 27 being the -----BEGIN CERTIFICATE----- length
			int end = pem.IndexOf ("-----END CERTIFICATE-----", start);
			if (end < start)
				return null;
				
			string base64 = pem.Substring (start, (end - start));
			return Convert.FromBase64String (base64);
		}

		public void ProcessRequest (int reqId, string verb, string queryString, string path,
					string protocol, string localAddress, int serverPort, string remoteAddress,
					int remotePort, string remoteName, string [] headers, string [] headerValues)
		{
			ModMonoRequestBroker broker = (ModMonoRequestBroker) RequestBroker;
			ModMonoWorkerRequest mwr = new ModMonoWorkerRequest (reqId, broker, this, verb, path, queryString,
									protocol, localAddress, serverPort, remoteAddress,
									remotePort, remoteName, headers, headerValues);
			if (mwr.IsSecure ()) {
				// note: we're only setting what we use (and not the whole lot)
				mwr.AddServerVariable ("CERT_KEYSIZE", broker.GetServerVariable (reqId, "SSL_CIPHER_USEKEYSIZE"));
				mwr.AddServerVariable ("CERT_SECRETKEYSIZE", broker.GetServerVariable (reqId, "SSL_CIPHER_ALGKEYSIZE"));

				string pem_cert = broker.GetServerVariable (reqId, "SSL_CLIENT_CERT");
				// 52 is the minimal PEM size for certificate header/footer
				if ((pem_cert != null) && (pem_cert.Length > 52)) {
					byte[] certBytes = FromPEM (pem_cert);
					mwr.SetClientCertificate (certBytes);

					// check client certificate validity with Apache and/or Mono
					if (mwr.IsClientCertificateValid (certBytes)) {
						// client cert present (bit0 = 1) and valid (bit1 = 0)
						mwr.AddServerVariable ("CERT_FLAGS", "1");
					} else {
						// client cert present (bit0 = 1) but invalid (bit1 = 1)
						mwr.AddServerVariable ("CERT_FLAGS", "3");
					}
				} else {
					mwr.AddServerVariable ("CERT_FLAGS", "0");
				}

				pem_cert = broker.GetServerVariable (reqId, "SSL_SERVER_CERT");
				// 52 is the minimal PEM size for certificate header/footer
				if ((pem_cert != null) && (pem_cert.Length > 52)) {
					byte[] certBytes = FromPEM (pem_cert);
					X509Certificate cert = new X509Certificate (certBytes);
					mwr.AddServerVariable ("CERT_SERVER_ISSUER", cert.GetIssuerName ());
					mwr.AddServerVariable ("CERT_SERVER_SUBJECT", cert.GetName ());
				}
			}

			ProcessRequest (mwr);
		}
	}
	
	//
	// ModMonoWorker: The worker that do the initial processing of mod_mono
	// requests.
	//
	internal class ModMonoWorker: Worker
	{
		ApplicationServer server;
		public LingeringNetworkStream Stream;
		ModMonoRequest modRequest;
		bool closed;

		public ModMonoWorker (Socket client, ApplicationServer server)
		{
			Stream = new LingeringNetworkStream (client, true);
			Stream.EnableLingering = false;
			this.server = server;
		}

		int requestId = -1;
		ModMonoRequestBroker broker = null;
			
		public override void Run (object state)
		{
			try {
				InnerRun (state);
			} catch (FileNotFoundException fnf) {
				// We print this one, as it might be a sign of a bad deployment
				// once we require the .exe and Mono.WebServer in bin or the GAC.
				Console.Error.WriteLine (fnf);
			} catch (IOException) {
				// This is ok (including EndOfStreamException)
			} catch (Exception e) {
				Console.Error.WriteLine (e);
			} finally {
				try {
					// Closing is enough for mod_mono. the module will return a 50x
					Stream.Close ();
					Stream = null;
				} catch {}

				if (broker != null && requestId != -1) {
					broker.UnregisterRequest (requestId);
					requestId = -1;
				}
			}
		}

		VPathToHost GetOrCreateApplication (string vhost, int port, string filepath, string virt)
		{
			VPathToHost vapp = null;
			string vdir = Path.GetDirectoryName (virt);
			string pdir = Path.GetDirectoryName (filepath);
			DirectoryInfo vinfo = new DirectoryInfo (vdir);
			DirectoryInfo pinfo = new DirectoryInfo (pdir);
			string final_pdir = null;
			string final_vdir = null;
			while (vinfo != null && pinfo != null) {
				if (final_pdir == null && CheckDirectory (pinfo)) {
					final_pdir = pinfo.ToString ();
					final_vdir = vinfo.ToString ();
					break;
				}

				if (pinfo.Name != vinfo.Name) {
					final_vdir = vinfo.ToString ();
					break;
				}

				pinfo = pinfo.Parent;
				vinfo = vinfo.Parent;
			}

			if (final_pdir == null) {
				final_pdir = pinfo.ToString ();
			}

			if (final_vdir == null) {
				final_vdir = vinfo.ToString ();
			}


			//Console.Error.WriteLine ("final_pdir: {0} final_vdir: {1}", final_pdir, final_vdir);
			vapp = server.GetApplicationForPath (vhost, port, virt, false);
			if (vapp == null) {
				// Don't know why this breaks mod-mono-server2.exe, but not mod-mono-server.exe
				//final_pdir = "file://" + final_pdir;
				if (final_vdir [0] != '/')
					final_vdir = "/" + final_vdir;
				server.AddApplication (vhost, port, final_vdir, final_pdir);
				vapp = server.GetApplicationForPath (vhost, port, virt, false);
			}

			return vapp;
		}

		static bool CheckDirectory (DirectoryInfo info)
		{
			if (!info.Exists)
				return false;

			FileInfo [] g1 = info.GetFiles ("Global.asax");
			if (g1.Length != 0)
				return true;

			g1 = info.GetFiles ("global.asax");
			if (g1.Length != 0)
				return true;

			return (info.GetDirectories ("bin").Length != 0);
		}

		void InnerRun (object state)
		{
			requestId = -1;
			broker = null;
			
			RequestReader rr = new RequestReader (Stream);
			if (rr.ShuttingDown) {
				Close ();
				server.Stop ();
				return;
			}

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

			VPathToHost vapp = null;
			string vpath = rr.GetUriPath ();
			string path = rr.GetPhysicalPath ();
			if (path == null) {
				vapp = server.GetApplicationForPath (vhost, port, vpath, false);
			} else {
				vapp = GetOrCreateApplication (vhost, port, path, vpath);
			}

			if (vapp == null) {
				rr.Decline ();
				Stream.Close ();
				Stream = null;
				return;
			}

			ModMonoApplicationHost host = (ModMonoApplicationHost) vapp.AppHost;
			if (host == null) {
				rr.Decline ();
				Stream.Close ();
				Stream = null;
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
		}

		public override int Read (byte[] buffer, int position, int size)
		{
			return modRequest.GetClientBlock (buffer, position, size);
		}
		
		public override void Write (byte[] buffer, int position, int size)
		{
			modRequest.SendResponseFromMemory (buffer, position, size);
		}
		
		public override void Close ()
		{
			if (closed)
				return;

			closed = true;
			try {
				modRequest.Close ();
			} catch {} // ignore any error
			try {
				if (Stream != null) {
					Stream.Close ();
					Stream = null;
				}
			} catch {}
		}
		
		public override void Flush ()
		{
			//modRequest.Flush (); No-op
		}

		public override bool IsConnected ()
		{
			return modRequest.IsConnected ();
		}

		public void SendFile (string filename)
		{
			modRequest.SendFile (filename);
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

