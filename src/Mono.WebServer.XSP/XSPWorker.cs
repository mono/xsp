//
// Mono.WebServer.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
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
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Mono.Security.Protocol.Tls;
using Mono.WebServer.Log;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;

namespace Mono.WebServer.XSP
{
	//
	// XSPWorker: The worker that do the initial processing of XSP
	// requests.
	//
	class XSPWorker: Worker
	{
		readonly ApplicationServer server;
		readonly LingeringNetworkStream netStream;
		readonly Stream stream;
		readonly IPEndPoint remoteEP;
		readonly IPEndPoint localEP;
		readonly Socket sock;
		readonly SslInformation ssl;
		InitialWorkerRequest initial;
		int requestId = -1;
		XSPRequestBroker broker;
		int reuses;

		public XSPWorker (Socket client, EndPoint localEP, ApplicationServer server,
			bool secureConnection,
			SecurityProtocolType securityProtocol,
			X509Certificate cert,
			PrivateKeySelectionCallback keyCB,
			bool allowClientCert,
			bool requireClientCert) 
		{
			if (secureConnection) {
				ssl = new SslInformation {
					AllowClientCertificate = allowClientCert,
					RequireClientCertificate = requireClientCert,
					RawServerCertificate = cert.GetRawCertData ()
				};

				netStream = new LingeringNetworkStream (client, true);
				var s = new SslServerStream (netStream, cert, requireClientCert, false);
				s.PrivateKeyCertSelectionDelegate += keyCB;
				s.ClientCertValidationDelegate += ClientCertificateValidation;
				stream = s;
			} else {
				netStream = new LingeringNetworkStream (client, false);
				stream = netStream;
			}

			sock = client;
			this.server = server;
			remoteEP = (IPEndPoint) client.RemoteEndPoint;
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
			stream.BeginRead (buffer, 0, buffer.Length, ReadCB, buffer);
		}

		void ReadCB (IAsyncResult ares)
		{
			var buffer = (byte []) ares.AsyncState;
			try {
				int nread = stream.EndRead (ares);
				// See if we got at least 1 line
				initial.SetBuffer (buffer, nread);
				initial.ReadRequestData ();
				ThreadPool.QueueUserWorkItem (RunInternal);
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
			int port = localEP.Port;
			VPathToHost vapp; 

			try {
				vapp = server.GetApplicationForPath (vhost, port, rdata.Path, true);
			} catch (Exception e){
				//
				// This happens if the assembly is not in the GAC, so we report this
				// error here.
				//
				Logger.Write (e);
				return;
			}
			
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
				var s = (stream as SslServerStream);
				ssl.KeySize = s.CipherStrength;
				ssl.SecretKeySize = s.KeyExchangeStrength;
			}

			try {
				string redirect;
				vapp.Redirect (rdata.Path, out redirect);
				host.ProcessRequest (requestId, localEP,
						     remoteEP, rdata.Verb,
						     rdata.Path, rdata.QueryString,
						     rdata.Protocol, rdata.InputBuffer, redirect, sock.Handle, ssl);
			} catch (FileNotFoundException fnf) {
				// We print this one, as it might be a sign of a bad deployment
				// once we require the .exe and Mono.WebServer in bin or the GAC.
				Logger.Write (fnf);
			} catch (IOException) {
				// This is ok (including EndOfStreamException)
			} catch (Exception e) {
				Logger.Write (e);
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
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Peer unexpectedly closed the connection on write. Closing our end.");
				Logger.Write (ex);
				Close ();
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
				} else if (!netStream.OwnsSocket) {
					try {
						if (sock.Connected && !(sock.Poll (0, SelectMode.SelectRead) && sock.Available == 0))
							sock.Shutdown (SocketShutdown.Both);
					} catch {
						// ignore
					}
					try {
						sock.Close ();
					} catch {
						// ignore
					}
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
			return !ssl.RequireClientCertificate || (certificate != null);
		}
	}
}
