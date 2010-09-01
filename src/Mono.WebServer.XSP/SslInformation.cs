//
// Mono.WebServer.SslInfomation
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004-2010 Novell, Inc. (http://www.novell.com)
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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Protocol.Tls;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Mono.WebServer
{
	[Serializable]
	[Obsolete ("This class should not be used. It will be removed from Mono.WebServer.dll")]
	public class SslInformation {
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
}
