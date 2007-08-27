//
// Mono.WebServer.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004,2005,2006 Novell, Inc. (http://www.novell.com)
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Protocol.Tls;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Mono.WebServer
{
	//
	// XSPWebSource: Provides methods to get objects and types specific
	// to XSP.
	//
	public class XSPWebSource: WebSource
	{
		IPEndPoint bindAddress;
		bool secureConnection;
		SecurityProtocolType SecurityProtocol;
		X509Certificate cert;
		PrivateKeySelectionCallback keyCB;
		bool allowClientCert;
		bool requireClientCert;

		public XSPWebSource(IPAddress address, int port, SecurityProtocolType securityProtocol,
					X509Certificate cert, PrivateKeySelectionCallback keyCB, 
					bool allowClientCert, bool requireClientCert)
		{			
			secureConnection = (cert != null && keyCB != null);
			this.bindAddress = new IPEndPoint (address, port);
			this.SecurityProtocol = securityProtocol;
			this.cert = cert;
			this.keyCB = keyCB;
			this.allowClientCert = allowClientCert;
			this.requireClientCert = requireClientCert;
		}

		public XSPWebSource (IPAddress address, int port)
		{
			SetListenAddress (address, port);
		}
		
		public void SetListenAddress (int port)
		{
			SetListenAddress (IPAddress.Any, port);
		}

		public void SetListenAddress (IPAddress address, int port) 
		{
			SetListenAddress (new IPEndPoint (address, port));
		}
		
		public void SetListenAddress (IPEndPoint bindAddress)
		{
			if (bindAddress == null)
				throw new ArgumentNullException ("bindAddress");

			this.bindAddress = bindAddress;
		}
		
		public override Socket CreateSocket ()
		{
			Socket listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (bindAddress);
			return listen_socket;
		} 

		public override Worker CreateWorker (Socket client, ApplicationServer server)
		{
			return new XSPWorker (client, client.LocalEndPoint, server,
				secureConnection, SecurityProtocol, cert, keyCB, allowClientCert, requireClientCert);
		}
		
		public override Type GetApplicationHostType ()
		{
			return typeof (XSPApplicationHost);
		}
		
		public override IRequestBroker CreateRequestBroker ()
		{
			return new XSPRequestBroker ();
		}
	}
	
	//
	// XSPRequestBroker: The request broker for XSP. Provides some
	// additional methods to the BaseRequestBroker from which inherits.
	//
	public class XSPRequestBroker: BaseRequestBroker
	{
		public int GetReuseCount (int requestId)
		{
			XSPWorker worker = (XSPWorker) GetWorker (requestId);
			if (worker == null)
				return 0;
			
			return worker.GetRemainingReuses ();
		}

		public void Close (int requestId, bool keepAlive)
		{
			XSPWorker worker = (XSPWorker) GetWorker (requestId);
			if (worker != null) {
				try {
					worker.Close (keepAlive);
				} catch {}
			}
		}
	}
	
	//
	// XSPApplicationHost: The application host for XSP.
	//
	public class XSPApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (int reqId, long localEPAddr, int localEPPort, long remoteEPAdds,
					int remoteEPPort, string verb, string path,
					string queryString, string protocol, byte [] inputBuffer, string redirect,
					IntPtr socket, SslInformations ssl)
		{
			XSPRequestBroker broker = (XSPRequestBroker) RequestBroker;
			IPEndPoint localEP = new IPEndPoint (localEPAddr, localEPPort);
			IPEndPoint remoteEP = new IPEndPoint (remoteEPAdds, remoteEPPort);
			bool secure = (ssl != null);
			XSPWorkerRequest mwr = new XSPWorkerRequest (reqId, broker, this, localEP, remoteEP, verb, path,
								queryString, protocol, inputBuffer, socket, secure);

			if (secure) {
				// note: we're only setting what we use (and not the whole lot)
				mwr.AddServerVariable ("CERT_KEYSIZE", ssl.KeySize.ToString (CultureInfo.InvariantCulture));
				mwr.AddServerVariable ("CERT_SECRETKEYSIZE", ssl.SecretKeySize.ToString (CultureInfo.InvariantCulture));
 
				if (ssl.RawClientCertificate != null) {
					// the worker need to be able to return it (if asked politely)
					mwr.SetClientCertificate (ssl.RawClientCertificate);

					// XSPWorkerRequest will answer, as required, for CERT_COOKIE, CERT_ISSUER, 
					// CERT_SERIALNUMBER and CERT_SUBJECT (as anyway it requires the client 
					// certificate - if it was provided)

					if (ssl.ClientCertificateValid) {
						// client cert present (bit0 = 1) and valid (bit1 = 0)
						mwr.AddServerVariable ("CERT_FLAGS", "1");
					} else {
						// client cert present (bit0 = 1) but invalid (bit1 = 1)
						mwr.AddServerVariable ("CERT_FLAGS", "3");
					}
				} else {
					// no client certificate (bit0 = 0) ? does bit1 matter ?
					mwr.AddServerVariable ("CERT_FLAGS", "0");
				}

				if (ssl.RawServerCertificate != null) {
					X509Certificate server = ssl.GetServerCertificate ();
					mwr.AddServerVariable ("CERT_SERVER_ISSUER", server.GetIssuerName ());
					mwr.AddServerVariable ("CERT_SERVER_SUBJECT", server.GetName ());
				}
			}

			string translated = mwr.GetFilePathTranslated ();
			if (path [path.Length - 1] != '/' && Directory.Exists (translated))
				redirect = path + '/';

			if (redirect != null) {
				Redirect (mwr, redirect);
				broker.UnregisterRequest (reqId);
				return;
			}

			ProcessRequest (mwr);
		}

		static string content301 = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
				"<html><head>\n<title>301 Moved Permanently</title>\n</head><body>\n" +
				"<h1>Moved Permanently</h1>\n" +
				"<p>The document has moved to <a href='http://{0}{1}'>http://{0}{1}</a>.</p>\n" +
				"</body></html>\n";

		static void Redirect (XSPWorkerRequest wr, string location)
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

	[Serializable]
	public class SslInformations {
		bool client_cert_allowed;
		bool client_cert_required;
		bool client_cert_valid;
		byte[] raw_client_cert;
		byte[] raw_server_cert;
		int key_size;
		int secret_key_size;

		public bool AllowClientCertificate {
			get { return client_cert_allowed; }
			set { client_cert_allowed = value; }
		}

		public bool RequireClientCertificate {
			get { return client_cert_required; }
			set { client_cert_required = value; }
		}

		public bool ClientCertificateValid {
			get { return client_cert_valid; }
			set { client_cert_valid = value; }
		}

		public int KeySize {
			get { return key_size; }
			set { key_size = value; }
		}			

		public byte[] RawClientCertificate {
			get { return raw_client_cert; }
			set { raw_client_cert = value; }
		}

		public byte[] RawServerCertificate {
			get { return raw_server_cert; }
			set { raw_server_cert = value; }
		}

		public int SecretKeySize {
			get { return secret_key_size; }
			set { secret_key_size = value; }
		}

		public X509Certificate GetServerCertificate ()
		{
			if (raw_server_cert == null)
				return null;

			return new X509Certificate (raw_server_cert);
		}
	}
	
	//
	// XSPWorker: The worker that do the initial processing of XSP
	// requests.
	//
	class XSPWorker: Worker
	{
		ApplicationServer server;
		LingeringNetworkStream netStream;
		Stream stream;
		IPEndPoint remoteEP;
		IPEndPoint localEP;
		Socket sock;
		SslInformations ssl;
		InitialWorkerRequest initial;
		int requestId = -1;
		XSPRequestBroker broker = null;
		int reuses;

		public XSPWorker (Socket client, EndPoint localEP, ApplicationServer server,
			bool secureConnection,
			Mono.Security.Protocol.Tls.SecurityProtocolType SecurityProtocol,
			X509Certificate cert,
			PrivateKeySelectionCallback keyCB,
			bool allowClientCert,
			bool requireClientCert) 
		{
			if (secureConnection) {
				ssl = new SslInformations ();
				ssl.AllowClientCertificate = allowClientCert;
				ssl.RequireClientCertificate = requireClientCert;
				ssl.RawServerCertificate = cert.GetRawCertData ();

				netStream = new LingeringNetworkStream (client, true);
				SslServerStream s = new SslServerStream (netStream, cert, requireClientCert, false);
				s.PrivateKeyCertSelectionDelegate += keyCB;
				s.ClientCertValidationDelegate += new CertificateValidationCallback (ClientCertificateValidation);
				stream = s;
			} else {
				netStream = new LingeringNetworkStream (client, false);
				stream = netStream;
			}

			sock = client;
			this.server = server;
			this.remoteEP = (IPEndPoint) client.RemoteEndPoint;
			this.localEP = (IPEndPoint) localEP;
		}

		public override bool IsAsync {
			get { return true; }
		}

		public override void SetReuseCount (int nreuses)
		{
			reuses = nreuses;
		}

		public override int GetRemainingReuses ()
		{
			return 100 - reuses;
		}

		public override void Run (object state)
		{
			initial = new InitialWorkerRequest (stream);
			byte [] buffer = InitialWorkerRequest.AllocateBuffer ();
			stream.BeginRead (buffer, 0, buffer.Length, new AsyncCallback (ReadCB), buffer);
		}

		void ReadCB (IAsyncResult ares)
		{
			byte [] buffer = (byte []) ares.AsyncState;
			try {
				int nread = stream.EndRead (ares);
				// See if we got at least 1 line
				initial.SetBuffer (buffer, nread);
				initial.ReadRequestData ();
				ThreadPool.QueueUserWorkItem (new WaitCallback (RunInternal));
			} catch (Exception e) {
				InitialWorkerRequest.FreeBuffer (buffer);
				HandleInitialException (e);
			}
		}

		void HandleInitialException (Exception e)
		{
			//bool ignore = ((e is RequestLineException) || (e is IOException));
			//if (!ignore)
			//	Console.WriteLine (e);

			try {
				if (initial != null && initial.GotSomeInput && sock.Connected) {
					byte [] error = HttpErrors.ServerError ();
					Write (error, 0, error.Length);
				}
			} catch {}

			try {
				Close ();
			} catch {}

			if (broker != null && requestId != -1)
				broker.UnregisterRequest (requestId);
		}

		void RunInternal (object state)
		{
			RequestData rdata = initial.RequestData;
			initial.FreeBuffer ();
			string vhost = null; // TODO: read the headers in InitialWorkerRequest
			int port = ((IPEndPoint) localEP).Port;
			
			VPathToHost vapp = server.GetApplicationForPath (vhost, port, rdata.Path, true);
			XSPApplicationHost host = null;
			if (vapp != null)
				host = (XSPApplicationHost) vapp.AppHost;

			if (host == null) {
				byte [] nf = HttpErrors.NotFound (rdata.Path);
				Write (nf, 0, nf.Length);
				Close ();
				return;
			}
			
			broker = (XSPRequestBroker) vapp.RequestBroker;
			requestId = broker.RegisterRequest (this);

			if (ssl != null) {
				SslServerStream s = (stream as SslServerStream);
				ssl.KeySize = s.CipherStrength;
				ssl.SecretKeySize = s.KeyExchangeStrength;
			}

			try {
				string redirect;
				vapp.Redirect (rdata.Path, out redirect);
				host.ProcessRequest (requestId, localEP.Address.Address, localEP.Port,
						remoteEP.Address.Address, remoteEP.Port, rdata.Verb,
						rdata.Path, rdata.QueryString,
						rdata.Protocol, rdata.InputBuffer, redirect, sock.Handle, ssl);
			} catch (FileNotFoundException fnf) {
				// We print this one, as it might be a sign of a bad deployment
				// once we require the .exe and Mono.WebServer in bin or the GAC.
				Console.Error.WriteLine (fnf);
			} catch (IOException) {
				// This is ok (including EndOfStreamException)
			} catch (Exception e) {
				Console.Error.WriteLine (e);
			}
		}

		public override int Read (byte[] buffer, int position, int size)
		{
			int n = stream.Read (buffer, position, size);
			return (n >= 0) ? n : -1;
		}
		
		public override void Write (byte[] buffer, int position, int size)
		{
			try {
				stream.Write (buffer, position, size);
			} catch (Exception) {
				Close ();
				throw;
			}
		}
		
		public override void Close ()
		{
			Close (false);
		}

		public void Close (bool keepAlive)
		{
			if (!keepAlive || !IsConnected ()) {
				stream.Close ();
				if (stream != netStream) {
					netStream.Close ();
				} else if (false == netStream.OwnsSocket) {
					try { sock.Close (); } catch {}
				}

				return;
			}

			netStream.EnableLingering = false;
			stream.Close ();
			if (stream != netStream)
				netStream.Close ();

			server.ReuseSocket (sock, reuses + 1);
		}

		public override bool IsConnected ()
		{
			return netStream.Connected;
		}

		public override void Flush ()
		{
			if (stream != netStream)
				stream.Flush ();
		}

		bool ClientCertificateValidation (X509Certificate certificate, int [] certificateErrors)
		{
			if (certificate != null)
				ssl.RawClientCertificate = certificate.GetRawCertData (); // to avoid serialization

			// right now we're accepting any client certificate - i.e. it's up to the 
			// web application to check if the certificate is valid (HttpClientCertificate.IsValid)
			ssl.ClientCertificateValid = (certificateErrors.Length == 0);
			return ssl.RequireClientCertificate ? (certificate != null) : true;
		}
	}
}

