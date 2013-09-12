//
// Mono.WebServer.MonoWorkerRequest
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Documentation:
//	Brian Nickel
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
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
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Hosting;
using Mono.WebServer.Log;

namespace Mono.WebServer
{	
	public abstract class MonoWorkerRequest : SimpleWorkerRequest
	{
		const string DEFAULT_EXCEPTION_HTML = "<html><head><title>Runtime Error</title></head><body>An exception ocurred:<pre>{0}</pre></body></html>";
		static readonly char[] mapPathTrimStartChars = { '/' };

		static bool checkFileAccess = true;
		readonly static bool needToReplacePathSeparator;
		readonly static char pathSeparatorChar;
		
		readonly IApplicationHost appHostBase;
		Encoding encoding;
		Encoding headerEncoding;
		byte [] queryStringBytes;
		string hostVPath;
		string hostPath;
		string hostPhysicalRoot;
		EndOfSendNotification end_send;
		object end_send_data;
		X509Certificate client_cert;
		NameValueCollection server_variables;
		bool inUnhandledException;
		
 		// as we must have the client certificate (if provided) then we're able to avoid
 		// pre-calculating some items (and cache them if we have to calculate)
 		string cert_cookie;
 		string cert_issuer;
 		string cert_serial;
 		string cert_subject;

		protected byte[] server_raw;
		protected byte[] client_raw;

		public event MapPathEventHandler MapPathEvent;
		public event EndOfRequestHandler EndOfRequestEvent;

		public abstract int RequestId { get; }

		protected static bool RunningOnWindows { get; private set; }

		public static bool CheckFileAccess {
			get { return checkFileAccess; }
			set { checkFileAccess = value; }
		}
		
		// Gets the physical path of the application host of the
		// current instance.
		string HostPath {
			get {
				if (hostPath == null)
					hostPath = appHostBase.Path;

				return hostPath;
			}
		}

		// Gets the virtual path of the application host of the
		// current instance.
		string HostVPath {
			get {
				if (hostVPath == null)
					hostVPath = appHostBase.VPath;

				return hostVPath;
			}
		}

		string HostPhysicalRoot {
			get {
				if (hostPhysicalRoot == null)
					hostPhysicalRoot = appHostBase.Server.PhysicalRoot;

				return hostPhysicalRoot;
			}
		}
		
		protected virtual Encoding Encoding {
			get {
				if (encoding == null)
					encoding = Encoding.GetEncoding (28591);

				return encoding;
			}
			set {
				encoding = value;
			}
		}

		protected virtual Encoding HeaderEncoding {
			get {
				if (headerEncoding == null) {
					HttpContext ctx = HttpContext.Current;
					HttpResponse response = ctx != null ? ctx.Response : null;
					Encoding enc = inUnhandledException ? null :
						response != null ? response.HeaderEncoding : null;
					headerEncoding = enc ?? Encoding;
				}
				return headerEncoding;
			}
		}
		
		static MonoWorkerRequest ()
		{
			PlatformID pid = Environment.OSVersion.Platform;
			RunningOnWindows = ((int) pid != 128 && pid != PlatformID.Unix && pid != PlatformID.MacOSX);

			if (Path.DirectorySeparatorChar != '/') {
				needToReplacePathSeparator = true;
				pathSeparatorChar = Path.DirectorySeparatorChar;
			}
			
			try {
				string v = ConfigurationManager.AppSettings ["MonoServerCheckHiddenFiles"];
				if (!String.IsNullOrEmpty (v)) {
					if (!Boolean.TryParse (v, out checkFileAccess))
						checkFileAccess = true;
				}
			} catch (Exception) {
				// ignore
				checkFileAccess = true;
			}
		}

		protected MonoWorkerRequest (IApplicationHost appHost)
			: base (String.Empty, String.Empty, null)
		{
			if (appHost == null)
				throw new ArgumentNullException ("appHost");

			appHostBase = appHost;
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
			if (String.IsNullOrEmpty (hostHeader)) {
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

		// Invokes the registered delegates one by one until the path is mapped.
		//
		// Parameters:
		//    path = virutal path of the request.
		//
		// Returns a string containing the mapped physical path of the request, or null if
		// the path was not successfully mapped.
		//
		string DoMapPathEvent (string path)
		{
			if (MapPathEvent != null) {
				var args = new MapPathEventArgs (path);
				foreach (MapPathEventHandler evt in MapPathEvent.GetInvocationList ()) {
					evt (this, args);
					if (args.IsMapped)
						return args.MappedPath;
				}
			}

			return null;
		}

		// The logic here is as follows:
		//
		// If path is equal to the host's virtual path (including trailing slash),
		// return the host virtual path.
		//
		// If path is absolute (starts with '/') then check if it's under our host vpath. If
		// it is, base the mapping under the virtual application's physical path. If it
		// isn't use the physical root of the application server to return the mapped
		// path. If you have just one application configured, then the values computed in
		// both of the above cases will be the same. If you have several applications
		// configured for this xsp/mod-mono-server instance, then virtual paths outside our
		// application virtual path will return physical paths relative to the server's
		// physical root, not application's. This is consistent with the way IIS worker
		// request works. See bug #575600
		//
		public override string MapPath (string path)
		{
			string eventResult = DoMapPathEvent (path);
			if (eventResult != null)
				return eventResult;

			string hostVPath = HostVPath;
			int hostVPathLen = HostVPath.Length;
			int pathLen = path != null ? path.Length : 0;
#if NET_2_0
			bool inThisApp = path.StartsWith (hostVPath, StringComparison.Ordinal);
#else
			bool inThisApp = path.StartsWith (hostVPath);
#endif
			if (pathLen == 0 || (inThisApp && (pathLen == hostVPathLen || (pathLen == hostVPathLen + 1 && path [pathLen - 1] == '/')))) {
				if (needToReplacePathSeparator)
					return HostPath.Replace ('/', pathSeparatorChar);
				return HostPath;
			}

			string basePath = null;
			switch (path [0]) {
				case '~':
					if (path.Length >= 2 && path [1] == '/')
						path = path.Substring (1);
					break;

				case '/':
					if (!inThisApp)
						basePath = HostPhysicalRoot;
					break;
			}

			if (basePath == null)
				basePath = HostPath;
			
			if (inThisApp && (path.Length == hostVPathLen || path [hostVPathLen] == '/'))
				path = path.Substring (hostVPathLen + 1);
			
			path = path.TrimStart (mapPathTrimStartChars);
			if (needToReplacePathSeparator)
				path = path.Replace ('/', pathSeparatorChar);
			
			return Path.Combine (basePath, path);
		}

		protected abstract bool GetRequestData ();		

		public bool ReadRequestData ()
		{
			return GetRequestData ();
		}

		static void LocationAccessible (string localPath)
		{
			bool doThrow = false;
			
			if (RunningOnWindows) {
				try {
					var fi = new FileInfo (localPath);
					FileAttributes attr = fi.Attributes;

					if ((attr & FileAttributes.Hidden) != 0 || (attr & FileAttributes.System) != 0)
						doThrow = true;
				} catch (Exception) {
					// ignore, will be handled in system.web
					return;
				}
			} else {
				// throw only if the file exists, let system.web handle the request
				// otherwise 
				if (File.Exists (localPath) || Directory.Exists (localPath))
					if (Path.GetFileName (localPath) [0] == '.')
						doThrow = true;
			}

			if (doThrow)
				throw new HttpException (403, "Forbidden.");
		}
		
		void AssertFileAccessible ()
		{
			if (!checkFileAccess)
				return;
			
			string localPath = GetFilePathTranslated ();
			if (String.IsNullOrEmpty (localPath))
				return;

			char dirsep = Path.DirectorySeparatorChar;
			string appPath = GetAppPathTranslated ();
			string[] segments = localPath.Substring (appPath.Length).Split (dirsep);

			var sb = new StringBuilder (appPath);
			foreach (string s in segments) {
				if (s.Length == 0)
					continue;
				
				if (s [0] != '.') {
					sb.Append (s);
					sb.Append (dirsep);
					continue;
				}

				sb.Append (s);
				LocationAccessible (sb.ToString ());
			}
		}

		public void ProcessRequest ()
		{
			string error = null;
			inUnhandledException = false;
			
			try {
				AssertFileAccessible ();
				HttpRuntime.ProcessRequest (this);
			} catch (HttpException ex) {
				inUnhandledException = true;
				error = ex.GetHtmlErrorMessage ();
			} catch (Exception ex) {
				inUnhandledException = true;
				var hex = new HttpException (400, "Bad request", ex);
				error = hex.GetHtmlErrorMessage ();
			}

			if (!inUnhandledException)
				return;
			
			if (error.Length == 0)
				error = String.Format (DEFAULT_EXCEPTION_HTML, "Unknown error");

			try {
				SendStatus (400, "Bad request");
				SendUnknownResponseHeader ("Connection", "close");
				SendUnknownResponseHeader ("Date", DateTime.Now.ToUniversalTime ().ToString ("r"));
				
				Encoding enc = Encoding.UTF8;
				
				byte[] bytes = enc.GetBytes (error);
				
				SendUnknownResponseHeader ("Content-Type", "text/html; charset=" + enc.WebName);
				SendUnknownResponseHeader ("Content-Length", bytes.Length.ToString ());
				SendResponseFromMemory (bytes, bytes.Length);
				FlushResponse (true);
			} catch (Exception ex) { // should "never" happen
				Logger.Write (LogLevel.Error, "Error while processing a request: ");
				Logger.Write (ex);
				throw;
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

			string headerName = GetKnownResponseHeaderName (index);
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

			var fileContent = new byte [8192];
			int count = fileContent.Length;
			while (length > 0 && (count = stream.Read (fileContent, 0, count)) != 0) {
				SendResponseFromMemory (fileContent, count);
				length -= count;
				// Keep the System. prefix
				count = (int) System.Math.Min (length, fileContent.Length);
			}
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			FileStream file = null;
			try {
				file = File.OpenRead (filename);
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
							cert_issuer = client.Issuer;
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
							cert_subject = client.Subject;
	 				}
					return cert_subject;
	 			}
			}

			string s = server_variables [name];
			return s ?? String.Empty;
		}

		public void AddServerVariable (string name, string value)
		{
			if (server_variables == null)
				server_variables = new NameValueCollection ();

			server_variables.Add (name, value);
		}

		#region Client Certificate Support

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
		
		#endregion

		protected static string[] SplitAndTrim (string list)
		{
			if (String.IsNullOrEmpty (list))
				return new string[0];
			return (from f in list.Split (',')
			        let trimmed =  f.Trim ()
			        where trimmed.Length != 0
			        select trimmed).ToArray ();
		}
	}
}

