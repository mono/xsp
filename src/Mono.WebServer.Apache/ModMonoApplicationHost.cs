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
using System.Security.Cryptography.X509Certificates;

namespace Mono.WebServer
{
	//
	// ModMonoApplicationHost: The application host for mod_mono.
	//
	public class ModMonoApplicationHost : BaseApplicationHost
	{
		static byte[] FromPEM (string pem) 
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
					    int remotePort, string remoteName, string [] headers, string [] headerValues, object worker)
		{
			ModMonoWorkerRequest mwr = null;

			try {
				if (reqId > -1) {
					mwr = new ModMonoWorkerRequest (reqId, (ModMonoRequestBroker) RequestBroker, this, verb, path, queryString,
									protocol, localAddress, serverPort, remoteAddress,
									remotePort, remoteName, headers, headerValues);
				} else {
					mwr = new ModMonoWorkerRequest (worker, this, verb, path, queryString,
									protocol, localAddress, serverPort, remoteAddress,
									remotePort, remoteName, headers, headerValues);
				}

				if (mwr.IsSecure ()) {
					// note: we're only setting what we use (and not the whole lot)
					mwr.AddServerVariable ("CERT_KEYSIZE", mwr.GetServerVariable (reqId, "SSL_CIPHER_USEKEYSIZE"));
					mwr.AddServerVariable ("CERT_SECRETKEYSIZE", mwr.GetServerVariable (reqId, "SSL_CIPHER_ALGKEYSIZE"));

					string pem_cert = mwr.GetServerVariable (reqId, "SSL_CLIENT_CERT");
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

					pem_cert = mwr.GetServerVariable (reqId, "SSL_SERVER_CERT");
					// 52 is the minimal PEM size for certificate header/footer
					if ((pem_cert != null) && (pem_cert.Length > 52)) {
						byte[] certBytes = FromPEM (pem_cert);
						var cert = new X509Certificate (certBytes);
						mwr.AddServerVariable ("CERT_SERVER_ISSUER", cert.Issuer);
						mwr.AddServerVariable ("CERT_SERVER_SUBJECT", cert.Subject);
					}
				}
			} catch (Exception) {
				EndOfRequest (mwr);
				throw;
			}
			
			ProcessRequest (mwr);
		}
	}
}
