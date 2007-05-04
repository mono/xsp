//
// MonoWorkerRequest.cs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace Mono.WebServer
{
	public class MapPathEventArgs : EventArgs
	{
		string path;
		string mapped;
		bool isMapped;

		public MapPathEventArgs (string path)
		{
			this.path = path;
			isMapped = false;
		}

		public string Path {
			get { return path; }
		}
		
		public bool IsMapped {
			get { return isMapped; }
		}

		public string MappedPath {
			get { return mapped; }
			set {
				mapped = value;
				isMapped = (value != null && value != "");
			}
		}
	}

	public delegate void MapPathEventHandler (object sender, MapPathEventArgs args);
	public delegate void EndOfRequestHandler (MonoWorkerRequest request);
	
	public abstract class MonoWorkerRequest : SimpleWorkerRequest
	{
		IApplicationHost appHostBase;
		Encoding encoding;
		Encoding headerEncoding;
		byte [] queryStringBytes;
		string hostVPath;
		string hostPath;
		EndOfSendNotification end_send;
		object end_send_data;

		protected byte[] server_raw;
		protected byte[] client_raw;
		X509Certificate client_cert;
		NameValueCollection server_variables;
		bool inUnhandledException;
		
		public MonoWorkerRequest (IApplicationHost appHost)
			: base (String.Empty, String.Empty, null)
		{
			if (appHost == null)
				throw new ArgumentNullException ("appHost");

			appHostBase = appHost;
		}

		public event MapPathEventHandler MapPathEvent;
		public event EndOfRequestHandler EndOfRequestEvent;
		
		string HostPath {
			get { 
				if (hostPath == null)
					hostPath = appHostBase.Path;

				return hostPath;
			}
		}

		string HostVPath {
			get { 
				if (hostVPath == null)
					hostVPath = appHostBase.VPath;

				return hostVPath;
			}
		}

		protected virtual Encoding Encoding {
			get {
				if (encoding == null)
					encoding = Encoding.GetEncoding (28591);

				return encoding;
			}

			set { encoding = value; }
		}

		protected virtual Encoding HeaderEncoding {
			get {
#if NET_2_0
				if (headerEncoding == null) {
					HttpContext ctx = HttpContext.Current;
					HttpResponse response = ctx != null ? ctx.Response : null;
					Encoding enc = inUnhandledException ? null :
						response != null ? response.HeaderEncoding : null;
					if (enc != null)
						headerEncoding = enc;
					else
						headerEncoding = this.Encoding;
				}
				return headerEncoding;
					
#else
				return this.Encoding;
#endif
			}
		}

		public override string GetAppPath ()
		{
			return HostVPath;
		}

		public override string GetAppPathTranslated ()
		{
			return HostPath;
		}

		public override string GetFilePathTranslated ()
		{
			return MapPath (GetFilePath ());
		}

		public override string GetLocalAddress ()
		{
			return "localhost";
		}

		public override string GetServerName ()
		{
			string hostHeader = GetKnownRequestHeader(HeaderHost);
			if (hostHeader == null || hostHeader.Length == 0) {
				hostHeader = GetLocalAddress ();
			} else {
				int colonIndex = hostHeader.IndexOf (':');
				if (colonIndex > 0) {
					hostHeader = hostHeader.Substring (0, colonIndex);
				} else if (colonIndex == 0) {
					hostHeader = GetLocalAddress ();
				}
			}
			return hostHeader;
		}


		public override int GetLocalPort ()
		{
			return 0;
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			return null;
		}

		public override byte [] GetQueryStringRawBytes ()
		{
			if (queryStringBytes == null) {
				string queryString = GetQueryString ();
				if (queryString != null)
					queryStringBytes = Encoding.GetBytes (queryString);
			}

			return queryStringBytes;
		}

		string DoMapPathEvent (string path)
		{
			if (MapPathEvent != null) {
				MapPathEventArgs args = new MapPathEventArgs (path);
				foreach (MapPathEventHandler evt in MapPathEvent.GetInvocationList ()) {
					evt (this, args);
					if (args.IsMapped)
						return args.MappedPath;
				}
			}

			return null;
		}
		
		public override string MapPath (string path)
		{
			string eventResult = DoMapPathEvent (path);
			if (eventResult != null)
				return eventResult;

			if (path == null || path.Length == 0 || path == HostVPath)
				return HostPath.Replace ('/', Path.DirectorySeparatorChar);

			if (path [0] == '~' && path.Length > 2 && path [1] == '/')
				path = path.Substring (1);

			int len = HostVPath.Length;
			if (path.StartsWith (HostVPath) && (path.Length == len || path [len] == '/'))
				path = path.Substring (len + 1);

			while (path.Length > 0 && path [0] == '/') {
				path = path.Substring (1);
			}

			if (Path.DirectorySeparatorChar != '/')
				path = path.Replace ('/', Path.DirectorySeparatorChar);

			return Path.Combine (HostPath, path);
		}

		protected abstract bool GetRequestData ();
		public abstract int RequestId { get; }

		public bool ReadRequestData ()
		{
			return GetRequestData ();
		}

		static readonly string defaultExceptionHtml = "<html><head><title>Runtime Error</title></head><body>An exception ocurred:<pre>{0}</pre></body></html>";
		
		public void ProcessRequest ()
		{
			string error = null;
			inUnhandledException = false;
			
			try {
				HttpRuntime.ProcessRequest (this);
			} catch (HttpException ex) {
				inUnhandledException = true;
				error = ex.GetHtmlErrorMessage ();
			} catch (Exception ex) {
				inUnhandledException = true;
				HttpException hex = new HttpException (400, "Bad request", ex);
				if (hex != null) // just a precaution
					error = hex.GetHtmlErrorMessage ();
				else
					error = String.Format (defaultExceptionHtml, ex.Message);
			}

			if (!inUnhandledException)
				return;
			
			if (error.Length == 0)
				error = String.Format (defaultExceptionHtml, "Unknown error");

			try {
				SendStatus (400, "Bad request");
				SendUnknownResponseHeader ("Connection", "close");
				SendUnknownResponseHeader ("Date", DateTime.Now.ToUniversalTime ().ToString ("r"));
				
				Encoding enc = Encoding.UTF8;
				if (enc == null)
					enc = Encoding.ASCII;
				
				byte[] bytes = enc.GetBytes (error);
				
				SendUnknownResponseHeader ("Content-Type", "text/html; charset=" + enc.WebName);
				SendUnknownResponseHeader ("Content-Length", bytes.Length.ToString ());
				SendResponseFromMemory (bytes, bytes.Length);
				FlushResponse (true);
			} catch (Exception ex) { // should "never" happen
				throw ex;
			}
		}

		public override void EndOfRequest ()
		{
			if (EndOfRequestEvent != null)
				EndOfRequestEvent (this);

			if (end_send != null)
				end_send (this, end_send_data);
		}
		
		public override void SetEndOfSendNotification (EndOfSendNotification callback, object extraData)
		{
			end_send = callback;
			end_send_data = extraData;
		}

		public override void SendCalculatedContentLength (int contentLength)
		{
			//FIXME: Should we ignore this for apache2?
			SendUnknownResponseHeader ("Content-Length", contentLength.ToString ());
		}

		public override void SendKnownResponseHeader (int index, string value)
		{
			if (HeadersSent ())
				return;

			string headerName = HttpWorkerRequest.GetKnownResponseHeaderName (index);
			SendUnknownResponseHeader (headerName, value);
		}

		protected void SendFromStream (Stream stream, long offset, long length)
		{
			if (offset < 0 || length <= 0)
				return;
			
			long stLength = stream.Length;
			if (offset + length > stLength)
				length = stLength - offset;

			if (offset > 0)
				stream.Seek (offset, SeekOrigin.Begin);

			byte [] fileContent = new byte [8192];
			int count = fileContent.Length;
			while (length > 0 && (count = stream.Read (fileContent, 0, count)) != 0) {
				SendResponseFromMemory (fileContent, count);
				length -= count;
				count = (int) System.Math.Min (length, fileContent.Length);
			}
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			FileStream file = null;
			try {
				file = File.OpenRead (filename);
				SendResponseFromFile (file.Handle, offset, length);
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

 		// as we must have the client certificate (if provided) then we're able to avoid
 		// pre-calculating some items (and cache them if we have to calculate)
 		private string cert_cookie;
 		private string cert_issuer;
 		private string cert_serial;
 		private string cert_subject;
 
		public override string GetServerVariable (string name)
		{
			if (server_variables == null)
				return String.Empty;

			if (IsSecure ()) {
	 			X509Certificate client = ClientCertificate;
	 			switch (name) {
 				case "CERT_COOKIE":
 					if (cert_cookie == null) {
 						if (client == null)
 							cert_cookie = String.Empty;
 						else
 							cert_cookie = client.GetCertHashString ();
 					}
 					return cert_cookie;
	 			case "CERT_ISSUER":
	 				if (cert_issuer == null) {
	 					if (client == null)
	 						cert_issuer = String.Empty;
	 					else
	 						cert_issuer = client.GetIssuerName ();
	 				}
	 				return cert_issuer;
	 			case "CERT_SERIALNUMBER":
	 				if (cert_serial == null) {
	 					if (client == null)
	 						cert_serial = String.Empty;
	 					else
	 						cert_serial = client.GetSerialNumberString ();
	 				}
	 				return cert_serial;
	 			case "CERT_SUBJECT":
	 				if (cert_subject == null) {
	 					if (client == null)
	 						cert_subject = String.Empty;
	 					else
	 						cert_subject = client.GetName ();
	 				}
					return cert_subject;
	 			}
			}

			string s = server_variables [name];
			return (s == null) ? String.Empty : s;
		}

		public void AddServerVariable (string name, string value)
		{
			if (server_variables == null)
				server_variables = new NameValueCollection ();

			server_variables.Add (name, value);
		}

		// Client Certificate Support

		public X509Certificate ClientCertificate {
			get {
				if ((client_cert == null) && (client_raw != null))
					client_cert = new X509Certificate (client_raw);
				return client_cert;
			}
		}

		public void SetClientCertificate (byte[] rawcert)
		{
			client_raw = rawcert;
		}

		public override byte[] GetClientCertificate ()
		{
			return client_raw;
		}
		
		public override byte[] GetClientCertificateBinaryIssuer ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateBinaryIssuer ();
			// TODO: not 100% sure of the content
			return new byte [0];
		}
		
		public override int GetClientCertificateEncoding ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateEncoding ();
			return 0;
		}
		
		public override byte[] GetClientCertificatePublicKey ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificatePublicKey ();
			return ClientCertificate.GetPublicKey ();
		}

		public override DateTime GetClientCertificateValidFrom ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateValidFrom ();
			return DateTime.Parse (ClientCertificate.GetEffectiveDateString ());
		}

		public override DateTime GetClientCertificateValidUntil ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateValidUntil ();
			return DateTime.Parse (ClientCertificate.GetExpirationDateString ());
		}
	}
}

