//
// ModMonoWorkerRequest.cs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//	Marek Habersack <grendel@twistedcode.net>
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
// (C) Copyright 2004-2008 Novell, Inc. (http://www.novell.com)
// Copyright 2012 Xamarin, Inc (http://xamarin.com)
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
using System.Collections.Generic;
using System.Web;
using System.Collections;
using System.Globalization;
using System.IO;
using Mono.Security.X509;
using Mono.Security.X509.Extensions;
using Mono.WebServer.Apache;
using Mono.WebServer.Log;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace Mono.WebServer
{
	public class ModMonoWorkerRequest : MonoWorkerRequest
	{
		bool closed;
		readonly string verb;
		readonly string queryString;
		readonly string protocol;
		string path;
		string pathInfo;
		readonly string rawUrl;
		readonly string localAddress;
		readonly int serverPort;
		readonly string remoteAddress;
		readonly int remotePort;
		readonly string remoteName;
		readonly ModMonoRequestBroker requestBroker;
		readonly ModMonoWorker worker;
		readonly string[] headers;
		int [] headersHash;
		readonly string[] headerValues;
		readonly int requestId;
		bool gotSecure;
		bool isSecure;
		readonly IApplicationHost appHost;
		
		// client certificate validity support
		string cert_hash;
		bool cert_validity;
		readonly static bool cert_check_apache;
		readonly static bool cert_check_mono;

		string [][] unknownHeaders;
		static string [] indexFiles = { "index.aspx",
						"Default.aspx",
						"default.aspx",
						"index.html",
						"index.htm" };

		static ModMonoWorkerRequest ()
		{
			try {
				string indexes = ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"];
				SetDefaultIndexFiles (indexes);
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Worker initialization exception occurred. Continuing anyway:\n{0}", ex);
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

			indexFiles = SplitAndTrim (list);
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
			this.appHost = appHost;
			rawUrl = path;
			if (!String.IsNullOrEmpty (queryString))
				rawUrl += "?" + queryString;
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

		public ModMonoWorkerRequest (object worker,
					IApplicationHost appHost, string verb, string path,
					string queryString, string protocol, string localAddress,
					int serverPort, string remoteAddress, int remotePort,
					string remoteName, string[] headers, string[] headerValues)
			: this (-1, null, appHost, verb, path, queryString, protocol, localAddress,
				serverPort, remoteAddress, remotePort, remoteName, headers, headerValues)
		{
			this.worker = (ModMonoWorker) worker;
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
			return rawUrl;
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
				pathInfo = String.Empty;
				return true;
			}

			string old_path = path;
			Paths.GetPathsFromUri (appHost, verb, old_path, out path, out pathInfo);
			return true;
		}
		
		public override bool HeadersSent ()
		{
			if (requestId == -1)
				return worker.HeadersSent;
			return requestBroker.GetHeadersSent (requestId);
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
				string val = GetServerVariable (requestId, "SERVER_PORT_SECURE");
				isSecure =  !String.IsNullOrEmpty (val);
				gotSecure = true;
			}

			return isSecure;
		}

		public string GetServerVariable (int requestId, string name)
		{
			if (requestId == -1)
				return worker.GetServerVariable (name);
			if (requestBroker != null)
				return requestBroker.GetServerVariable (requestId, name);
			return null;
		}

		bool IsClientCertificateValidForApache ()
		{
			string val = GetServerVariable (requestId, "SSL_CLIENT_VERIFY");
			if (String.IsNullOrEmpty (val))
				return false;
			return (val.Trim () == "SUCCESS");
		}

		static bool CheckClientCertificateExtensions (X509Certificate cert)
		{
			const KeyUsages ku = KeyUsages.digitalSignature | KeyUsages.keyEncipherment | KeyUsages.keyAgreement;
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
			}
			if (kux != null) {
				return kux.Support (ku);
			}
			if (eku != null) {
				// Client Authentication (1.3.6.1.5.5.7.3.2)
				return eku.KeyPurpose.Contains ("1.3.6.1.5.5.7.3.2");
			}

			// last chance - try with older (deprecated) Netscape extensions
			xtn = cert.Extensions["2.16.840.1.113730.1.1"];
			if (xtn != null) {
				var ct = new NetscapeCertTypeExtension (xtn);
				return ct.Support (NetscapeCertTypeExtension.CertTypes.SslClient);
			}

			// certificate isn't valid for SSL client usage
			return false;
		}

		static bool CheckChain (X509Certificate cert)
		{
			return new X509Chain ().Build (cert);
		}

		bool IsCertificateValidForMono (byte[] der)
		{
			var cert = new X509Certificate (der);
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
			if (closed) 
				return;
			if (requestId == -1) {
				try {
					worker.Close ();
				} finally {
					worker.Dispose ();
				}
			} else 
				requestBroker.Close (requestId);
			closed = true;
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

		readonly Hashtable server_vars = new Hashtable (StringComparer.InvariantCultureIgnoreCase);
		public override string GetServerVariable (string name)
		{
			object o = server_vars [name];
			if (o is bool)
				return null;

			if (o != null)
				return (string) o;

			string result = GetServerVariable (requestId, name);
			if (!String.IsNullOrEmpty (result)) {
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
				if (String.IsNullOrEmpty (result))
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

			if (response == null)
				return;
			if (requestId == -1)
				worker.SetOutputBuffering (response.BufferOutput);
			else
				requestBroker.SetOutputBuffering (requestId, response.BufferOutput);
		}
		
		public override void SendResponseFromMemory (IntPtr data, int length)
		{
			if (requestId > -1) {
				base.SendResponseFromMemory (data, length);
				return;
			}

			worker.Write (data, length);
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			UpdateModMonoConfig ();
			
			if (requestId > -1 && data.Length > length * 2) {
				// smaller buffer when using remoting
				var tmpbuffer = new byte [length];
				Buffer.BlockCopy (data, 0, tmpbuffer, 0, length);
				requestBroker.Write (requestId, tmpbuffer, 0, length);
			} else {
				if (requestId == -1)
					worker.Write (data, 0, length);
				else
					requestBroker.Write (requestId, data, 0, length);
			}
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			UpdateModMonoConfig ();
			string line = String.Format("{0} {1}", statusCode, statusDescription);
			if (requestId == -1)
				worker.SetStatusCodeLine (statusCode, line);
			else
				requestBroker.SetStatusCodeLine (requestId, statusCode, line);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			if (requestId == -1)
				worker.SetResponseHeader (name, value);
			else
				requestBroker.SetResponseHeader (requestId, name, value);
		}

		public override bool IsClientConnected ()
		{
			if (requestId == -1)
				return worker.IsConnected ();
			return (requestBroker != null && requestBroker.IsConnected (requestId));
		}

		public override string GetUriPath ()
		{
			string result = path;
			if (!String.IsNullOrEmpty (pathInfo))
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
				var pairs = new List<string[]> ();
				for (int i = 0; i < count; i++) {
					if (GetKnownRequestHeaderIndex (headers [i]) != -1)
						continue;

					pairs.Add (new[] { headers [i], headerValues [i]});
				}
				
				if (pairs.Count != 0) {
					unknownHeaders = new string [pairs.Count][];
					for (int i = 0; i < pairs.Count; i++)
						unknownHeaders [i] = pairs [i];
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
			StringComparer hp = StringComparer.InvariantCultureIgnoreCase;
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

			if (requestId > -1) {
				byte [] readBuffer;
				int nr = requestBroker.Read (requestId, size, out readBuffer);
				if (nr > 0 && readBuffer != null)
					Buffer.BlockCopy (readBuffer, 0, buffer, 0, nr);
				return nr;
			}
			return worker.Read (buffer, 0, size);
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			if (offset == 0) {
				var info = new FileInfo (filename);
				if (info.Length == length) {
					if (requestId == -1)
						worker.SendFile (filename);
					else
						requestBroker.SendFile (requestId, filename);
					return;
				}
			}

			FileStream file = null;
			try {
				file = File.OpenRead (filename);
				// We must not call the SendResponseFromFile overload which
				// takes  IntPtr in this case since it will callthe base
				// implementation of that overload which, in turn, will
				// close the handle (as it uses FileStream to wrap the
				// handle we pass). This will cause the handle to be closed
				// twice (FileStream owns the handle). So we just take a
				// shortcut to what the base overload does here.
				SendFromStream (file, offset, length);
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

