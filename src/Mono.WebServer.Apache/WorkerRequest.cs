//
// ModMonoWorkerRequest.cs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
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
using System.Web;
using System.Collections;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Mono.Security.X509;
using Mono.Security.X509.Extensions;

namespace Mono.WebServer
{
	public class RequestReader
	{
		ModMonoRequest request;

		public ModMonoRequest Request {
			get { return request; }
		}
		
		public RequestReader (NetworkStream ns)
		{
			this.request = new ModMonoRequest (ns);
		}

		public string GetUriPath ()
		{
			string path = request.GetUri ();

			int dot = path.LastIndexOf ('.');
			int slash = (dot != -1) ? path.IndexOf ('/', dot) : 0;
			if (dot > 0 && slash > 0)
				path = path.Substring (0, slash);

			return path;
		}

		public string GetPhysicalPath ()
		{
			return request.GetPhysicalPath ();
		}

		public void Decline ()
		{
			request.Decline ();
		}

		public void NotFound ()
		{
			request.NotFound ();
		}

		public bool ShuttingDown {
			get { return request.ShuttingDown; }
		}
	}

	public class ModMonoWorkerRequest : MonoWorkerRequest
	{
		bool closed;
		string verb;
		string queryString;
		string protocol;
		string path;
		string pathInfo;
		string localAddress;
		int serverPort;
		string remoteAddress;
		int remotePort;
		string remoteName;
		ModMonoRequestBroker requestBroker;
		string[] headers;
		int [] headersHash;
		string[] headerValues;
		int requestId;
		bool gotSecure;
		bool isSecure;

		// client certificate validity support
		string cert_hash;
		bool cert_validity;
		static bool cert_check_apache;
		static bool cert_check_mono;

		string [][] unknownHeaders;
		static string [] indexFiles = { "index.aspx",
						"Default.aspx",
						"default.aspx",
						"index.html",
						"index.htm" };

		static ModMonoWorkerRequest ()
		{
			try {
#if NET_2_0
				string indexes = ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"];
#else
				string indexes = ConfigurationSettings.AppSettings ["MonoServerDefaultIndexFiles"];
#endif
				SetDefaultIndexFiles (indexes);
			} catch (Exception ex) {
				Console.WriteLine ("Worker initialization exception occurred. Continuing anyway:\n{0}", ex);
			}

			// by default the client certificate validity (CCV) checks are done by both Apache and Mono
			// but this can be limited to either Apache or Mono using the MOD_MONO_CCV environment variable
			string ccv = Environment.GetEnvironmentVariable ("MOD_MONO_CCV");
			if (ccv != null)
				ccv = ccv.ToLower (CultureInfo.InvariantCulture);
			switch (ccv) {
			case "mono":
				cert_check_mono = true;
				break;
			case "apache":
				cert_check_apache = true;
				break;
			default: // both
				cert_check_apache = true;
				cert_check_mono = true;
				break;
			}
		}

		static void SetDefaultIndexFiles (string list)
		{
			if (list == null)
				return;

			ArrayList files = new ArrayList ();
			string [] fs = list.Split (',');
			foreach (string f in fs) {
				string trimmed = f.Trim ();
				if (trimmed == "") 
					continue;

				files.Add (trimmed);
			}

			indexFiles = (string []) files.ToArray (typeof (string));
		}

		public ModMonoWorkerRequest (int requestId, ModMonoRequestBroker requestBroker,
					IApplicationHost appHost, string verb, string path,
					string queryString, string protocol, string localAddress,
					int serverPort, string remoteAddress, int remotePort,
					string remoteName, string[] headers, string[] headerValues)
			: base (appHost)
		{
			this.requestId = requestId;
			this.requestBroker = requestBroker;
			this.verb = verb;
			//this.protocol = protocol;
			// Don't let System.Web know if it's 1.1. This way apache handles the chunked
			// encoding for us, without sys.web interfering.
			this.protocol = "HTTP/1.0";
			this.queryString = queryString;
			this.path = path;
			this.localAddress = localAddress;
			this.serverPort = serverPort;
			this.remoteAddress = remoteAddress;
			this.remotePort = remotePort;
			this.remoteName = remoteName;
			this.headers = headers;
			this.headerValues = headerValues;
		}

		public override int RequestId {
			get { return requestId; }
		}

		public override string GetPathInfo ()
		{
			return pathInfo;
		}

		public override string GetRawUrl ()
		{
			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			if (queryString != null && queryString.Length > 0)
				return result + "?" + queryString;

			return result;
		}

		bool TryDirectory ()
		{
			string localPath = GetFilePathTranslated ();
			
			if (!Directory.Exists (localPath))
				return false;

			string oldPath = path;
			if (!path.EndsWith ("/"))
				path += "/";

			bool catOne = false;
			foreach (string indexFile in indexFiles) {
				string testfile = Path.Combine (localPath, indexFile);
				if (File.Exists (testfile)) {
					path += indexFile;
					catOne = true;
					break;
				}
			}

			if (!catOne)
				path = oldPath;

			return true;
		}

		protected override bool GetRequestData ()
		{
			if (TryDirectory ()) {
				pathInfo = "";
				return true;
			}

			string old_path = path;
			Paths.GetPathsFromUri (old_path, out path, out pathInfo);
			if (path [path.Length - 1] == '/')
				path = path + indexFiles [0];
			return true;
		}
		
		public override bool HeadersSent ()
		{
			//FIXME!!!!: how do we know this?
			return false;
		}
		
		public override void FlushResponse (bool finalFlush)
		{
			// FLUSH is a no-op in mod_mono. Apache takes care of it.
			// requestBroker.Flush (requestId);
			if (finalFlush)
				CloseConnection ();
		}

		public override bool IsSecure ()
		{
			if (!gotSecure) {
				string val = requestBroker.GetServerVariable (requestId, "SERVER_PORT_SECURE");
				isSecure =  (val != null && val != "");
				gotSecure = true;
			}

			return isSecure;
		}

		private bool IsClientCertificateValidForApache ()
		{
			string val = requestBroker.GetServerVariable (requestId, "SSL_CLIENT_VERIFY");
			if ((val == null) || (val.Length == 0))
				return false;
			return (val.Trim () == "SUCCESS");
		}

		private bool CheckClientCertificateExtensions (X509Certificate cert)
		{
			KeyUsages ku = KeyUsages.digitalSignature | KeyUsages.keyEncipherment | KeyUsages.keyAgreement;
			KeyUsageExtension kux = null;
			ExtendedKeyUsageExtension eku = null;

			X509Extension xtn = cert.Extensions["2.5.29.15"];
			if (xtn != null)
				kux = new KeyUsageExtension (xtn);

			xtn = cert.Extensions["2.5.29.37"];
			if (xtn != null)
				eku = new ExtendedKeyUsageExtension (xtn);

			if ((kux != null) && (eku != null)) {
				// RFC3280 states that when both KeyUsageExtension and 
				// ExtendedKeyUsageExtension are present then BOTH should
				// be valid
				return (kux.Support (ku) &&
					eku.KeyPurpose.Contains ("1.3.6.1.5.5.7.3.2"));
			} else if (kux != null) {
				return kux.Support (ku);
			} else if (eku != null) {
				// Client Authentication (1.3.6.1.5.5.7.3.2)
				return eku.KeyPurpose.Contains ("1.3.6.1.5.5.7.3.2");
			}

			// last chance - try with older (deprecated) Netscape extensions
			xtn = cert.Extensions["2.16.840.1.113730.1.1"];
			if (xtn != null) {
				NetscapeCertTypeExtension ct = new NetscapeCertTypeExtension (xtn);
				return ct.Support (NetscapeCertTypeExtension.CertTypes.SslClient);
			}

			// certificate isn't valid for SSL client usage
			return false;
		}

		private bool CheckChain (X509Certificate cert)
		{
			return new X509Chain ().Build (cert);
		}

		private bool IsCertificateValidForMono (byte[] der)
		{
			X509Certificate cert = new X509Certificate (der);
			// invalidate cache if the certificate validity period has ended
			if (cert.ValidUntil > DateTime.UtcNow)
				cert_hash = null;

			// heavyweight process, cache result
			string hash = BitConverter.ToString (cert.Hash);
			if (hash != cert_hash) {
				try {
					cert_validity = CheckClientCertificateExtensions (cert) && CheckChain (cert);
					cert_hash = hash;
				}
				catch {
					cert_validity = false;
				}
			}
			return cert_validity;
		}

		// apache: Client certificate is valid if Apache is satisfied (SSL_CLIENT_VERIFY).
		// mono: Client certificate is valid if Mono is satisfied.
		// both: (Default) Client certificate is valid if BOTH Apache and Mono agree it is.
		public bool IsClientCertificateValid (byte[] der)
		{
			bool apache = true;
			// both or apache-only
			if (cert_check_apache) {
				apache = IsClientCertificateValidForApache ();
			}
			bool mono = true;
			// both or mono-only
			if (cert_check_mono) {
				mono = IsCertificateValidForMono (der);
			}
			return (apache && mono);
		}

		public override void CloseConnection ()
		{
			if (!closed) {
				requestBroker.Close (requestId);
				closed = true;
			}
		}

		public override string GetHttpVerbName ()
		{
			return verb;
		}

		public override string GetHttpVersion ()
		{
			return protocol;
		}

		public override string GetLocalAddress ()
		{
			return localAddress;
		}

		public override int GetLocalPort ()
		{
			return serverPort;
		}

		public override string GetQueryString ()
		{
			return queryString;
		}

		public override string GetRemoteAddress ()
		{
			return remoteAddress;
		}

		public override int GetRemotePort ()
		{
			return remotePort;
		}

		Hashtable server_vars = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant,
							CaseInsensitiveComparer.DefaultInvariant);
		public override string GetServerVariable (string name)
		{
			object o = server_vars [name];
			if (o is bool)
				return null;

			if (o != null)
				return (string) o;

			string result = requestBroker.GetServerVariable (requestId, name);
			if (result != null && result.Length > 0) {
				server_vars [name] = result;
				return result;
			}

			switch (name) {
			case "HTTPS":
				result = (IsSecure ()) ? "on" : "off";
				server_vars ["HTTPS"] = result;
				break;
			default:
				result = base.GetServerVariable (name);
				if (result == null || result.Length == 0)
					server_vars [name] = false;
				else
					server_vars [name] = result;
				break;
			}

			return result;
		}

		void UpdateModMonoConfig ()
		{
			// Reconfigure apache to be in synch with current application settings
			HttpContext ctx = HttpContext.Current;
			HttpResponse response = ctx != null ? ctx.Response : null;

			if (response != null)
				requestBroker.SetOutputBuffering (requestId, response.BufferOutput);
		}
		
		public override void SendResponseFromMemory (byte [] data, int length)
		{
			UpdateModMonoConfig ();
			
			if (data.Length > length * 2) {
				byte [] tmpbuffer = new byte [length];
				Buffer.BlockCopy (data, 0, tmpbuffer, 0, length);
				requestBroker.Write (requestId, tmpbuffer, 0, length);
			} else {
				requestBroker.Write (requestId, data, 0, length);
			}
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			UpdateModMonoConfig ();			
			requestBroker.SetStatusCodeLine (requestId, statusCode, String.Format("{0} {1}", statusCode, statusDescription));
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			requestBroker.SetResponseHeader (requestId, name, value);
		}

		public override bool IsClientConnected ()
		{
			return (requestBroker != null && requestBroker.IsConnected (requestId));
		}

		public override string GetUriPath ()
		{
			string result = path;
			if (pathInfo != null && pathInfo.Length > 0)
				result += pathInfo;

			return result;
		}

		public override string GetFilePath ()
		{
			return path;
		}
		
		public override string GetRemoteName ()
		{
			return remoteName;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			return GetRequestHeader (name);
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			if (unknownHeaders == null) {
				int count = headers.Length;
				ArrayList pairs = new ArrayList ();
				for (int i = 0; i < count; i++) {
					if (HttpWorkerRequest.GetKnownRequestHeaderIndex (headers [i]) != -1)
						continue;

					pairs.Add (new string [] { headers [i], headerValues [i]});
				}
				
				if (pairs.Count != 0) {
					unknownHeaders = new string [pairs.Count][];
					for (int i = 0; i < pairs.Count; i++)
						unknownHeaders [i] = (string []) pairs [i];
				}
			}

			return unknownHeaders;
		}

		public override string GetKnownRequestHeader (int index)
		{
			return GetRequestHeader (GetKnownRequestHeaderName (index));
		}
		
		string GetRequestHeader (string name)
		{
			IHashCodeProvider hp = CaseInsensitiveHashCodeProvider.DefaultInvariant;
			if (headersHash == null) {
				headersHash = new int [headers.Length];
				for (int i = 0; i < headers.Length; i++) {
					headersHash [i] = hp.GetHashCode (headers [i]);
				}
			}

			int k = Array.IndexOf (headersHash, hp.GetHashCode (name));
			return (k == -1) ? null : headerValues [k];
		}

		public override void SendCalculatedContentLength (int contentLength) 
		{
			// Do nothing
		}

		public override int ReadEntityBody (byte [] buffer, int size)
		{
			if (buffer == null || size <= 0)
				return 0;

			byte [] readBuffer;
			int nr = requestBroker.Read (requestId, size, out readBuffer);
			if (nr > 0 && readBuffer != null)
				Buffer.BlockCopy (readBuffer, 0, buffer, 0, nr);
			return nr;
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			if (offset == 0) {
				FileInfo info = new FileInfo (filename);
				if (info.Length == length) {
					requestBroker.SendFile (requestId, filename);
					return;
				}
			}

			FileStream file = null;
			try {
				file = File.OpenRead (filename);
				base.SendResponseFromFile (file.Handle, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			Stream file = null;
			try {
				file = new FileStream (handle, FileAccess.Read);
				SendFromStream (file, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}
	}
}

