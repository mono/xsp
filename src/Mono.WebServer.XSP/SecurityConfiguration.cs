//
// server.cs: Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2005 Novell, Inc. (http://www.novell.com)
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
using System.Collections;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Mono.Security.Authenticode;
using Mono.Security.Protocol.Tls;
using MSX = Mono.Security.X509;
using Mono.Security.X509.Extensions;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;

namespace Mono.WebServer.XSP {

	public class SecurityConfiguration {
		private bool enabled;
		private bool valid;
		private bool accept_client_certificates;
		private bool require_client_certificates;
		private RSA key;
		private SecurityProtocolType protocol;
		private X509Certificate x509;
		private string p12Filename;
		private string certFilename;
		private string keyFilename;
		private string password;


		public SecurityConfiguration ()
		{
			protocol = SecurityProtocolType.Default;
		}

		// properties

		public bool AcceptClientCertificates {
			get { return accept_client_certificates; }
			set { accept_client_certificates = value; }
		}

		public string CertificateFile {
			get { return certFilename; }
			set { certFilename = value; }
		}

		public bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}

		public bool RequireClientCertificates {
			get { return require_client_certificates; }
			set { require_client_certificates = value; }
		}

		public string Password {
			get { throw new CryptographicException ("Password is write-only."); }
			set { password = value; }
		}

		public string Pkcs12File {
			get { return p12Filename; }
			set { p12Filename = value; }
		}

		public RSA KeyPair {
			get {
				if (!valid)
					CheckSecurityContextValidity ();
				return key;
			}
		}

		public string PvkFile {
			get { return keyFilename; }
			set { keyFilename = value; }
		}

		public X509Certificate ServerCertificate {
			get {
				if (!valid)
					CheckSecurityContextValidity ();
				return x509;
			}
		}

		public SecurityProtocolType Protocol {
			get { return protocol; }
			set { protocol = value; }
		}

		// methods

		public void CheckSecurityContextValidity ()
		{
			if (enabled) {
				// give priority to pkcs12 (for conflicting options)
				LoadPkcs12File (p12Filename, password);
				LoadCertificate (certFilename);
				LoadPrivateKeyFile (keyFilename, password);
			}
			valid = true;
		}

		public override string ToString ()
		{
			if (!enabled)
				return "(non-secure)";

			StringBuilder sb = new StringBuilder ("(");
			switch (protocol) {
			case SecurityProtocolType.Default:
				sb.Append ("auto-detect SSL3/TLS1");
				break;
			case SecurityProtocolType.Ssl3:
				sb.Append ("SSL3");
				break;
			case SecurityProtocolType.Tls:
				sb.Append ("TLS1");
				break;
			}
			if (require_client_certificates)
				sb.Append (" with mandatory client certificates");
			sb.Append (")");
			return sb.ToString ();
		}

		public void SetProtocol (string protocol)
		{
			if (protocol != null) {
				try {
					this.protocol = (SecurityProtocolType) Enum.Parse (typeof (SecurityProtocolType), protocol);
				}
				catch (Exception e) {
					string message = String.Format ("The value '{0}' given for security protocol is invalid.", protocol);
					throw new CryptographicException (message, e);
				}
			} else {
				this.protocol = SecurityProtocolType.Default;
			}
		}

		// private stuff

		private void LoadCertificate (string filename)
		{
			if ((filename == null) || (x509 != null))
				return;

			try {
				x509 = X509Certificate.CreateFromCertFile (filename);
			}
			catch (Exception e) {
				string message = String.Format ("Unable to load X.509 certicate file '{0}'.", filename);
				throw new CryptographicException (message, e);
			}
		}

		private void LoadPrivateKeyFile (string filename, string password)
		{
			if ((filename == null) || (key != null))
				return;

			try {
				if (password == null)
					key = PrivateKey.CreateFromFile (filename).RSA;
				else
					key = PrivateKey.CreateFromFile (filename, password).RSA;
			}
			catch (CryptographicException ce) {
				string message = String.Format ("Invalid private key password or private key file '{0}' is corrupt.", filename);
				throw new CryptographicException (message, ce);
			}
			catch (Exception e) {
				string message = String.Format ("Unable to load private key '{0}'.", filename);
				throw new CryptographicException (message, e);
			}
		}

		private void LoadPkcs12File (string filename, string password)
		{
			if ((filename == null) || (key != null))
				return;

			try {
				MSX.PKCS12 pfx = null;
				if (password == null)
					pfx = MSX.PKCS12.LoadFromFile (filename);
				else
					pfx = MSX.PKCS12.LoadFromFile (filename, password);

				// a PKCS12 file may contain many certificates and keys so we must find 
				// the best one (i.e. not the first one)

				// test for only one certificate / keypair
				if (Simple (pfx))
					return;

				// next, we look for a certificate with the server authentication EKU (oid 
				// 1.3.6.1.5.5.7.3.1) and, if found, try to find a key associated with it
				if (ExtendedKeyUsage (pfx))
					return;

				// next, we look for a certificate with the KU matching the use of a private
				// key matching SSL usage and, if found, try to find a key associated with it
				if (KeyUsage (pfx))
					return;

				// next, we look for the old netscape extension
				if (NetscapeCertType (pfx))
					return;

				// finally we iterate all keys (not certificates) to find a certificate with the 
				// same public key
				if (BruteForce (pfx))
					return;

				// we don't merit to use SSL ;-)
				throw new Exception ("Couldn't find an appropriate certificate and private key to use for SSL.");
			}
			catch (CryptographicException ce) {
				string message = String.Format ("Invalid private key password or private key file '{0}' is corrupt.", filename);
				throw new CryptographicException (message, ce);
			}
			catch (Exception e) {
				string message = String.Format ("Unable to load private key '{0}'.", filename);
				throw new CryptographicException (message, e);
			}
		}

		private bool Simple (MSX.PKCS12 pfx)
		{
			int ncerts = pfx.Certificates.Count;
			if (ncerts == 0)
				throw new Exception ("No certificates are present in the PKCS#12 file.");

			int nkeys = pfx.Keys.Count;
			if (nkeys == 0)
				throw new Exception ("No keypair is present in the PKCS#12 file.");

			if (ncerts == 1) {
				MSX.X509Certificate cert = pfx.Certificates [0];
				// only one certificate, find matching key
				if (nkeys == 1) {
					// only one key
					key = (pfx.Keys [0] as RSA);
				} else {
					// many keys (strange case)
					key = GetKeyMatchingCertificate (pfx, cert);
				}
				// complete ?
				if ((key != null) && (cert != null)) {
					x509 = new X509Certificate (cert.RawData);
					return true;
				}
			}

			return false;
		}

		private bool ExtendedKeyUsage (MSX.PKCS12 pfx)
		{
			foreach (MSX.X509Certificate cert in pfx.Certificates) {
				MSX.X509Extension xtn = cert.Extensions ["2.5.29.37"];
				if (xtn == null)
					continue;

				ExtendedKeyUsageExtension eku = new ExtendedKeyUsageExtension (xtn);
				if (!eku.KeyPurpose.Contains ("1.3.6.1.5.5.7.3.1"))
					continue;

				key = GetKeyMatchingCertificate (pfx, cert);
				if (key == null)
					continue;

				x509 = new X509Certificate (cert.RawData);
				break;
			}

			// complete ?
			return ((x509 != null) && (key != null));
		}

		private bool KeyUsage (MSX.PKCS12 pfx)
		{
			foreach (MSX.X509Certificate cert in pfx.Certificates) {
				MSX.X509Extension xtn = cert.Extensions ["2.5.29.15"];
				if (xtn == null)
					continue;

				KeyUsageExtension ku = new KeyUsageExtension (xtn);
				if (!ku.Support (KeyUsages.digitalSignature) && !ku.Support (KeyUsages.keyEncipherment))
					continue;

				key = GetKeyMatchingCertificate (pfx, cert);
				if (key == null)
					continue;

				x509 = new X509Certificate (cert.RawData);
				break;
			}

			// complete ?
			return ((x509 != null) && (key != null));
		}

		private bool NetscapeCertType (MSX.PKCS12 pfx)
		{
			foreach (MSX.X509Certificate cert in pfx.Certificates) {
				MSX.X509Extension xtn = cert.Extensions ["2.16.840.1.113730.1.1"];
				if (xtn == null)
					continue;

				NetscapeCertTypeExtension ct = new NetscapeCertTypeExtension (xtn);
				if (!ct.Support (NetscapeCertTypeExtension.CertTypes.SslServer))
					continue;

				key = GetKeyMatchingCertificate (pfx, cert);
				if (key == null)
					continue;

				x509 = new X509Certificate (cert.RawData);
				break;
			}

			// complete ?
			return ((x509 != null) && (key != null));
		}

		private bool BruteForce (MSX.PKCS12 pfx)
		{
			foreach (object o in pfx.Keys) {
				key = (o as RSA);
				if (key == null)
					continue;

				string s = key.ToXmlString (false);
				foreach (MSX.X509Certificate cert in pfx.Certificates) {
					if (s == cert.RSA.ToXmlString (false))
						x509 = new X509Certificate (cert.RawData);
				}

				// complete ?
				if ((x509 != null) && (key != null))
					return true;
			}
			return false;
		}

		private RSA GetKeyMatchingCertificate (MSX.PKCS12 pfx, MSX.X509Certificate cert)
		{
			IDictionary attributes = pfx.GetAttributes (cert);
			return (pfx.GetAsymmetricAlgorithm (attributes) as RSA);
		}
	}
}
