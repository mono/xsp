//
// MonoWorkerRequest.cs
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
	/// <summary>
	///    This class extends <see cref="EventArgs" /> to provide arguments
	///    for <see cref="MapPathEventHandler" />.
	/// </summary>
	/// <remarks>
	///    When <see cref="MonoWorkerRequest.MapPathEvent" /> is called, the
	///    handler has an option of setting <see
	///    cref="MapPathEventArgs.MappedPath" /> to a mapped path.
	/// </remarks>
	public class MapPathEventArgs : EventArgs
	{
		/// <summary>
		///    Contains the virtual path, as used in the request.
		/// </summary>
		string path;
		
		/// <summary>
		///    Contains the physical "mapped" path.
		/// </summary>
		string mapped;
		
		/// <summary>
		///    Indicates whether or not the path has been mapped.
		/// </summary>
		bool isMapped;
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MapPathEventArgs" /> for a specified virtual path.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> containing the virtual path, as
		///    contained in the request.
		/// </param>
		public MapPathEventArgs (string path)
		{
			this.path = path;
			isMapped = false;
		}

		/// <summary>
		///    Gets the virtual path of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the virtual path of
		///    the current instance.
		/// </value>
		public string Path {
			get { return path; }
		}
		
		/// <summary>
		///    Gets whether or not the path is mapped.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not the path
		///    has been mapped.
		/// </value>
		public bool IsMapped {
			get { return isMapped; }
		}

		/// <summary>
		///    Gets and sets the physical "mapped" path for the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the physical "mapped"
		///    path of the current instance.
		/// </value>
		public string MappedPath {
			get { return mapped; }
			set {
				mapped = value;
				isMapped = (value != null && value != "");
			}
		}
	}

	
	/// <summary>
	///    This delegate is used to handle <see
	///    cref="MonoWorkerRequest.MapPathEvent" /> and performs custom path
	///    mapping.
	/// </summary>
	/// <param name="sender">
	///    The <see cref="object" /> that sent the event.
	/// </param>
	/// <param name="args">
	///    A <see cref="MapPathEventArgs" /> object containing the arguments
	///    for the event.
	/// </param>
	/// <remarks>
	///    This method is used for custom path mapping within <see
	///    cref="MonoWorkerRequest.MapPath" />.
	/// </remarks>
	/// <example>
	///    An example <see cref="MapPathEventHandler" />
	///    <code language="C#">
	///        void OnMapPathEvent (object sender, MapPathEventArgs args)
	///        {
	///            if (args.Path.StartsWith ("/blog"))
	///                args.MappedPath = @"C:\Documents and Settings\John Doe\My Documents\Visual Studio 2005\WebSites\blog";
	///        }
	///    </code>
	/// </example>
	public delegate void MapPathEventHandler (object sender, MapPathEventArgs args);
	
	/// <summary>
	///    This delegate is used to handle <see
	///    cref="MonoWorkerRequest.EndOfRequestEvent" />.
	/// </summary>
	/// <param name="request">
	///    The <see cref="MonoWorkerRequest" /> that sent the event.
	/// </param>
	public delegate void EndOfRequestHandler (MonoWorkerRequest request);
	
	/// <summary>
	///    This abstract class extends <see cref="SimpleWorkerRequest" />,
	///    adding support for security certificates and implementing methods
	///    for use with a web server.
	/// </summary>
	public abstract class MonoWorkerRequest : SimpleWorkerRequest
	{
		/// <summary>
		///    Contains the application host used by the current
		///    instance.
		/// </summary>
		IApplicationHost appHostBase;
		
		/// <summary>
		///    Contains the encoding used for content in the current
		///    instance.
		/// </summary>
		Encoding encoding;
		
		/// <summary>
		///    Contains the encoding used for headers in the current
		///    instance.
		/// </summary>
		Encoding headerEncoding;
		
		/// <summary>
		///    Contains a <see cref="byte[]" /> representation of the
		///    query string.
		/// </summary>
		/// <remarks>
		///    When <see cref="GetQueryStringRawBytes" /> is called, it
		///    stores the encoded query string in this property so it
		///    only has to be converted once.
		/// </remarks>
		byte [] queryStringBytes;
		
		/// <summary>
		///    Contains the host virtual path of the current instance as
		///    read from the application host.
		/// </summary>
		string hostVPath;
		
		/// <summary>
		///    Contains the host physical path of the current instance
		///    as read from the application host.
		/// </summary>
		string hostPath;
		
		/// <summary>
		///    Contains the <see cref="EndOfSendNotification" />
		///    callback to call once all data has been sent.
		/// </summary>
		EndOfSendNotification end_send;
		
		/// <summary>
		///    Contains the data to send to <see cref="end_send" />.
		/// </summary>
		object end_send_data;
		
		
		/// <summary>
		///    Contains the raw server certificate used for
		///    authenticating the current instance, if secure.
		/// </summary>
		protected byte[] server_raw;
		
		/// <summary>
		///    Contains the raw client certificate used for
		///    authenticating the current instance, if secure.
		/// </summary>
		protected byte[] client_raw;
		
		/// <summary>
		///    Contains the X509 client certificate used for
		///    authenticating the current instance, if secure.
		/// </summary>
		X509Certificate client_cert;
		
		/// <summary>
		///    Contains the server variables in the current instance.
		/// </summary>
		NameValueCollection server_variables;
		
		/// <summary>
		///    Indicates whether or not an unhandled exception has
		///    occurred while processing the request.
		/// </summary>
		/// <remarks>
		///    Being within an unhandled exception can cause problems
		///    when accessing properties of the <see
		///    cref="HttpResponse" />.
		/// </remarks>
		bool inUnhandledException;
		
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MonoWorkerRequest" /> for a specified application
		///    host.
		/// </summary>
		/// <param name="appHost">
		///    A <see cref="IApplicationHost" /> object containing the
		///    application host that created the new instance.
		/// </param>
		/// <remarks>
		///    <paramref name="appHost" /> <B>MUST</B> be the <see
		///    cref="IApplicationHost" /> that created the new
		///    instance so they will be in the same <see
		///    cref="AppDomain" />.
		/// </remarks>
		public MonoWorkerRequest (IApplicationHost appHost)
			: base (String.Empty, String.Empty, null)
		{
			if (appHost == null)
				throw new ArgumentNullException ("appHost");

			appHostBase = appHost;
		}

		/// <summary>
		///    This event is called by <see cref="MapPath" /> and is
		///    used for custom path mapping.
		/// </summary>
		/// <remarks>
		///    <para>See <see cref="MapPathEventHandler" /> for an
		///    example.</para>
		///    <note type="caution">
		///        <para>Handlers added to are not guaranteed to be
		///        called. The class will evoke the handlers in order
		///        until the path is mapped, and then stop.</para>
		///    </note>
		/// </remarks>
		public event MapPathEventHandler MapPathEvent;
		
		/// <summary>
		///    This event is called after the request has been completed
		///    and should be used by request brokers to perform final
		///    operations.
		/// </summary>
		public event EndOfRequestHandler EndOfRequestEvent;
		
		/// <summary>
		///    Gets the physical path of the application host of the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the physical path of
		///    the application host of the current instance.
		/// </value>
		string HostPath {
			get { 
				if (hostPath == null)
					hostPath = appHostBase.Path;

				return hostPath;
			}
		}

		/// <summary>
		///    Gets the virtual path of the application host of the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the virtual path of
		///    the application host of the current instance.
		/// </value>
		string HostVPath {
			get { 
				if (hostVPath == null)
					hostVPath = appHostBase.VPath;

				return hostVPath;
			}
		}

		/// <summary>
		///    Gets the content encoding used by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the content encoding
		///    used by the current instance.
		/// </value>
		protected virtual Encoding Encoding {
			get {
				if (encoding == null)
					encoding = Encoding.GetEncoding (28591);

				return encoding;
			}

			set { encoding = value; }
		}

		/// <summary>
		///    Gets the header encoding used by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the header encoding
		///    used by the current instance.
		/// </value>
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

		/// <summary>
		///    Gets the virtual host path of the file used by of the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the virtual host path
		///    of the file used by the current instance.
		/// </returns>
		public override string GetAppPath ()
		{
			return HostVPath;
		}

		/// <summary>
		///    Gets the physical host path of the file used by of the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the physical host path
		///    of the file used by the current instance.
		/// </returns>
		public override string GetAppPathTranslated ()
		{
			return HostPath;
		}

		/// <summary>
		///    Gets the mapped path of the file used by of the current
		///    instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the mapped path of the
		///    file used by the current instance.
		/// </returns>
		public override string GetFilePathTranslated ()
		{
			return MapPath (GetFilePath ());
		}

		/// <summary>
		///    Gets the local address of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the local address of
		///    the current instance.
		/// </returns>
		public override string GetLocalAddress ()
		{
			return "localhost";
		}

		/// <summary>
		///    Gets the server name of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the server name of
		///    the current instance.
		/// </returns>
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


		/// <summary>
		///    Gets the local port of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> containing the port number of the
		///    current instance.
		/// </returns>
		public override int GetLocalPort ()
		{
			return 0;
		}

		/// <summary>
		///    Gets the preloaded entity data for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="byte[]" /> containing the preloaded entity
		///    data contained from the request.
		/// </returns>
		/// <remarks>
		///    If the request was receiving data before being processed,
		///    entity (form) data may have been accumulated. This method
		///    allows that data to be read directly.
		/// </remarks>
		public override byte [] GetPreloadedEntityBody ()
		{
			return null;
		}

		/// <summary>
		///    Gets the bytes representing the query string of the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="byte[]" /> containing the bytes representing
		///    the query string.
		/// </returns>
		public override byte [] GetQueryStringRawBytes ()
		{
			if (queryStringBytes == null) {
				string queryString = GetQueryString ();
				if (queryString != null)
					queryStringBytes = Encoding.GetBytes (queryString);
			}

			return queryStringBytes;
		}

		/// <summary>
		///    Evokes the registered <see cref="MapPathEventHandler" />
		///    delegates one by one until the path is mapped.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> containing the virutal path of
		///    the request.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> containing the mapped physical
		///    path of the request, or <see langword="null" /> if the
		///    path was not successfully mapped.
		/// </returns>
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
		
		/// <summary>
		///    Maps the virtual path of the request to a physical path.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> containing the virutal path of
		///    the request.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> containing the mapped physical
		///    path of the request.
		/// </returns>
		/// <remarks>
		///    By default, the path will be mapped using the virtual and
		///    physical paths of the <see cref="IApplicationHost" />
		///    used to create the current instance. To override this,
		///    register a <see cref="MapPathEventHandler" /> with <see
		///    cref="MapPathEvent" />.
		/// </remarks>
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

		/// <summary>
		///    Gets the request data.
		/// </summary>
		/// <returns>
		///    A <see cref="bool" /> indicating whether or not the data
		///    was gotten successfully.
		/// </returns>
		protected abstract bool GetRequestData ();
		
		/// <summary>
		///    Gets the request ID as used by the <see
		///    cref="IApplicationHost" />'s request broker.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the request ID.
		/// </value>
		public abstract int RequestId { get; }

		/// <summary>
		///    Reads the request data.
		/// </summary>
		/// <returns>
		///    A <see cref="bool" /> indicating whether or not the data
		///    was read successfully.
		/// </returns>
		public bool ReadRequestData ()
		{
			return GetRequestData ();
		}

		/// <summary>
		///    Contains the default exception HTML to be used if all
		///    other renderers fail.
		/// </summary>
		/// <remarks>
		///    This string is to be passed into <see
		///    cref="String.Format(string,object)" /> where the
		///    exception is the second argument.
		/// </remarks>
		static readonly string defaultExceptionHtml = "<html><head><title>Runtime Error</title></head><body>An exception ocurred:<pre>{0}</pre></body></html>";
		
		
		/// <summary>
		///    Processes the request contained in the current instance.
		/// </summary>
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

		/// <summary>
		///    Does final processing after the request has been
		///    completed.
		/// </summary>
		public override void EndOfRequest ()
		{
			if (EndOfRequestEvent != null)
				EndOfRequestEvent (this);

			if (end_send != null)
				end_send (this, end_send_data);
		}
		
		/// <summary>
		///    Sets the end-of-status notification callback and its
		///    complementary data.
		/// </summary>
		/// <param name="callback">
		///    A <see cref="EndOfSendNotification" /> delegate to be
		///    called when the current instance is finished sending data
		///    to the response.
		/// </param>
		/// <param name="extraData">
		///    A <see cref="object" /> containing data to be sent to
		///    <paramref name="callback" /> when it is called.
		/// </param>
		public override void SetEndOfSendNotification (EndOfSendNotification callback, object extraData)
		{
			end_send = callback;
			end_send_data = extraData;
		}

		/// <summary>
		///    Sends the calculated content length of the response.
		/// </summary>
		/// <param name="contentLength">
		///    A <see cref="int" /> containing the content length of the
		///    response.
		/// </param>
		/// <remarks>
		///    Including the content length in the header allows the
		///    client to show download progress.
		/// </remarks>
		public override void SendCalculatedContentLength (int contentLength)
		{
			//FIXME: Should we ignore this for apache2?
			SendUnknownResponseHeader ("Content-Length", contentLength.ToString ());
		}

		/// <summary>
		///    Sends a known response header with a specified index and
		///    value.
		/// </summary>
		/// <param name="index">
		///    A <see cref="int" /> containing a known response header
		///    index.
		/// </param>
		/// <param name="value">
		///    A <see cref="string" /> containing the response value.
		/// </param>
		public override void SendKnownResponseHeader (int index, string value)
		{
			if (HeadersSent ())
				return;

			string headerName = HttpWorkerRequest.GetKnownResponseHeaderName (index);
			SendUnknownResponseHeader (headerName, value);
		}

		/// <summary>
		///    Sends a response directly from stream at a specified
		///    offset with a specified length.
		/// </summary>
		/// <param name="stream">
		///    A <see cref="Stream" /> object to send from.
		/// </param>
		/// <param name="offset">
		///    A <see cref="long" /> specifying at what seek position to
		///    start sending from.
		/// </param>
		/// <param name="length">
		///    A <see cref="long" /> specifying the number of bytes to
		///    send.
		/// </param>
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

		/// <summary>
		///    Sends a response directly from file at a specified offset
		///    with a specified length.
		/// </summary>
		/// <param name="filename">
		///    A <see cref="string" /> containing the name of the file
		///    to send from.
		/// </param>
		/// <param name="offset">
		///    A <see cref="long" /> specifying at what seek position to
		///    start sending from.
		/// </param>
		/// <param name="length">
		///    A <see cref="long" /> specifying the number of bytes to
		///    send.
		/// </param>
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

		/// <summary>
		///    Sends a response directly from a raw file descriptor at a
		///    specified offset with a specified length.
		/// </summary>
		/// <param name="handle">
		///    A <see cref="IntPtr" /> pointing to a raw file
		///    descriptor.
		/// </param>
		/// <param name="offset">
		///    A <see cref="long" /> specifying at what seek position to
		///    start sending from.
		/// </param>
		/// <param name="length">
		///    A <see cref="long" /> specifying the number of bytes to
		///    send.
		/// </param>
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
 		
 		/// <summary>
 		///    Contains the certificate cookie as used by <see
 		///    cref="GetServerVariable" />.
 		/// </summary>
 		private string cert_cookie;
 		
 		/// <summary>
 		///    Contains the certificate issuer as used by <see
 		///    cref="GetServerVariable" />.
 		/// </summary>
 		private string cert_issuer;
 		
 		/// <summary>
 		///    Contains the certificate serial as used by <see
 		///    cref="GetServerVariable" />.
 		/// </summary>
 		private string cert_serial;
 		
 		/// <summary>
 		///    Contains the certificate subject as used by <see
 		///    cref="GetServerVariable" />.
 		/// </summary>
 		private string cert_subject;
 
		/// <summary>
		///    Gets a server variable with a specified name from the
		///    current instance.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> containing the name of the
		///    server variable to get.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> containing the value of the
		///    server variable, or <see cref="String.Empty" /> if the
		///    variable was not found.
		/// </returns>
		/// <remarks>
		///    Server variables are like environment variables and
		///    contain name/value pairs of information.
		/// </remarks>
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

		/// <summary>
		///    Adds a server variable to the current instance.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> containing the name of the
		///    server variable to add.
		/// </param>
		/// <param name="value">
		///    A <see cref="string" /> containing the value of the
		///    server variable to add.
		/// </param>
		/// <remarks>
		///    Server variables are like environment variables and
		///    contain name/value pairs of information.
		/// </remarks>
		public void AddServerVariable (string name, string value)
		{
			if (server_variables == null)
				server_variables = new NameValueCollection ();

			server_variables.Add (name, value);
		}

		#region Client Certificate Support

		/// <summary>
		///    Gets the X509 client certificate used by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="X509Certificate" /> object containing the
		///    client certificate used by the current instance.
		/// </value>
		/// <remarks>
		///    This property should only be used if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public X509Certificate ClientCertificate {
			get {
				if ((client_cert == null) && (client_raw != null))
					client_cert = new X509Certificate (client_raw);
				return client_cert;
			}
		}

		/// <summary>
		///    Sets the raw client certificate used by the current
		///    instance.
		/// </summary>
		/// <param name="rawcert">
		///    A <see cref="byte[]" /> containing the raw client
		///    certificate used by the current instance.
		/// </param>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public void SetClientCertificate (byte[] rawcert)
		{
			client_raw = rawcert;
		}

		/// <summary>
		///    Gets the raw client certificate used by the current
		///    instance.
		/// </summary>
		/// <returns>
		///    A <see cref="byte[]" /> containing the raw client
		///    certificate used by the current instance.
		/// </returns>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public override byte[] GetClientCertificate ()
		{
			return client_raw;
		}
		
		/// <summary>
		///    Gets the binary issuer of the client certificate used by
		///    the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="byte[]" /> containing the binary issuer of
		///    the client certificate used by the current instance.
		/// </returns>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public override byte[] GetClientCertificateBinaryIssuer ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateBinaryIssuer ();
			// TODO: not 100% sure of the content
			return new byte [0];
		}
		
		/// <summary>
		///    Gets the encoding of the client certificate used by the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> indicating the encoding of the
		///    client certificate used by the current instance.
		/// </returns>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public override int GetClientCertificateEncoding ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateEncoding ();
			return 0;
		}
		
		/// <summary>
		///    Gets the public key of the client certificate used by the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="byte[]" /> containing the public key
		///    the client certificate used by the current instance.
		/// </returns>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public override byte[] GetClientCertificatePublicKey ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificatePublicKey ();
			return ClientCertificate.GetPublicKey ();
		}

		/// <summary>
		///    Gets the date and time the client certificate used by the
		///    current instance is valid from.
		/// </summary>
		/// <returns>
		///    A <see cref="DateTime" /> containing the date and time
		///    the client certificate used by the current instance is
		///    valid from.
		/// </returns>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public override DateTime GetClientCertificateValidFrom ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateValidFrom ();
			return DateTime.Parse (ClientCertificate.GetEffectiveDateString ());
		}

		/// <summary>
		///    Gets the date and time the client certificate used by the
		///    current instance is valid until.
		/// </summary>
		/// <returns>
		///    A <see cref="DateTime" /> containing the date and time
		///    the client certificate used by the current instance is
		///    valid until.
		/// </returns>
		/// <remarks>
		///    This method should only be called if <see
		///    cref="IsSecure" /> is <see langword="true" />.
		/// </remarks>
		public override DateTime GetClientCertificateValidUntil ()
		{
			if (ClientCertificate == null)
				return base.GetClientCertificateValidUntil ();
			return DateTime.Parse (ClientCertificate.GetExpirationDateString ());
		}
		
		#endregion
	}
}

