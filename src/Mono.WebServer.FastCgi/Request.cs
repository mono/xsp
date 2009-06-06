//
// Requests/Request.cs: Handles FastCGI requests.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Robert Jordan <robertj@gmx.net>
//
// Copyright (C) 2007 Brian Nickel
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
#if NET_2_0
using System.Collections.Generic;
#else
using System.Collections;
#endif
using System.IO;
using System.Text;
using IOPath = System.IO.Path;

namespace Mono.FastCgi {
	public class Request
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the request ID to use when sending data.
		/// </summary>
		private ushort requestID;
		
		/// <summary>
		///    Contains the <see cref="Connection" /> object object from
		///    which data is received and to which data will be sent.
		/// </summary>
		private Connection connection;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Request" /> with the specified request ID and
		///    connection.
		/// </summary>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the request ID of the
		///    new instance.
		/// </param>
		/// <param name="connection">
		///    A <see cref="Connection" /> object from which data is
		///    received and to which data will be sent.
		/// </param>
		public Request (ushort requestID, Connection connection)
		{
			this.requestID  = requestID;
			this.connection = connection;
		}
		#endregion
		
		
		
		#region Request Completion Handling
		
		// When a request is completed, the output and error streams
		// must be completed and the final farewell must be send so
		// the HTTP server can finish its response.
		
		/// <summary>
		///    Completes the request by closing any opened response and
		///    error streams and sending the EndRequest record to the
		///    client.
		/// </summary>
		/// <param name="appStatus">
		///    <para>A <see cref="int" /> containing the application
		///    status the request ended with.</para>
		///    <para>This is the same value as would be returned by a
		///    program on termination. On successful termination, this
		///    would be zero.</para>
		/// </param>
		/// <param name="protocolStatus">
		///    A <see cref="ProtocolStatus" /> containing the FastCGI
		///    protocol status with which the request is being ended.
		/// </param>
		/// <remarks>
		///    To close the request, this method calls <see
		///    cref="Connection.EndRequest" />, which additionally
		///    releases the resources so they can be garbage collected.
		/// </remarks>
		public void CompleteRequest (int appStatus,
		                             ProtocolStatus protocolStatus)
		{
			// Data is no longer needed.
			DataNeeded = false;
			
			// Close the standard output if it was opened.
			if (stdout_sent)
				SendStreamData (RecordType.StandardOutput,
					new byte [0], 0);
			
			// Close the standard error if it was opened.
			if (stderr_sent)
				SendStreamData (RecordType.StandardError,
					new byte [0], 0);
			
			connection.EndRequest (requestID, appStatus,
				protocolStatus);
		}
		
		/// <summary>
		///    Aborts the request by sending a message on the error
		///    stream, logging it, and completing the request with an
		///    application status of -1.
		/// </summary>
		/// <param name="message">
		///    A <see cref="string" /> containing the error message.
		/// </param>
		/// <param name="args">
		///    A <see cref="object[]" /> containing argument to insert
		///    into the message.
		/// </param>
		public void Abort (string message, params object [] args)
		{
			SendError (message);
			Logger.Write (LogLevel.Error,
				Strings.Request_Aborting, RequestID);
			Logger.Write (LogLevel.Error, message, args);
			CompleteRequest (-1, ProtocolStatus.RequestComplete);
		}
		
		/// <summary>
		///    Indicates whether or not data is needed by the current
		///    instance.
		/// </summary>
		private bool data_needed = true;
		
		/// <summary>
		///    Gets and sets whether or not data is still needed by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not data is
		///    still needed by the current instance.
		/// </value>
		/// <remarks>
		///    This value is used by the connection to determine whether
		///    or not it still needs to receive data for the request
		///    from the socket. As soon as a request has received all
		///    the necessary data, it should set the value to <see
		///    langword="false" /> so the connection can continue on
		///    with its next task.
		/// </remarks>
		public bool DataNeeded {
			get {return data_needed;}
			protected set {data_needed = value;}
		}
		
		/// <summary>
		///    Gets the server that spawned the connection used by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="FastCgi.Server" /> containing the server
		///    that spawned the connection used by the current instance.
		/// </value>
		public Server Server {
			get {return connection.Server;}
		}
		
		/// <summary>
		///    Gets the request ID of the current instance as used by
		///    the connection.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> containing the request ID of the
		///    current instance.
		/// </value>
		public ushort RequestID {
			get {return requestID;}
		}
		
		/// <summary>
		///    Gets whether or not the connection used by the current
		///    instance is connected.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not the
		///    connection used by the current instance is connected.
		/// </value>
		/// <remarks>
		///    If the connection is not connected, any response data is
		///    disregarded. As such, before any intense operation, this
		///    value should be checked as to avoid any unneccessary
		///    work.
		/// </remarks>
		public bool IsConnected {
			get {return connection.IsConnected;}
		}
		#endregion
		
		
		
		#region Request Details
		
		/// <summary>
		///    Contains the host name, after <see cref="HostName" /> is
		///    called.
		/// </summary>
		private string vhost = null;
		
		/// <summary>
		///    Contains the port number, after <see cref="PortNumber" />
		///    is called.
		/// </summary>
		private int port = -1;
		
		/// <summary>
		///    Contains the path, after <see cref="Path" /> is called.
		/// </summary>
		private string path = null;
		
		/// <summary>
		///    Contains the physical path, after <see
		///    cref="PhysicalPath" /> is called.
		/// </summary>
		private string rpath = null;
		
		/// <summary>
		///    Gets the host name used to make the request handled by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the host name used to
		///    make the request handled by the current instance.
		/// </value>
		public string HostName {
			get {
				if (vhost == null)
					vhost = GetParameter ("HTTP_HOST");
				
				return vhost;
			}
		}
		
		/// <summary>
		///    Gets the port number used to make the request handled by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the port number used to
		///    make the request handled by the current instance.
		/// </value>
		public int PortNumber {
			get {
				if (port < 0)
					port = int.Parse (GetParameter (
							"SERVER_PORT"));
				
				return port;
			}
		}
		
		/// <summary>
		///    Gets the virtual path used to make the request handled by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the virtual path used
		///    to make the request handled by the current instance.
		/// </value>
		public string Path {
			get {
				if (path == null)
					path = GetParameter ("SCRIPT_NAME");
				
				return path;
			}
		}
		
		/// <summary>
		///    Gets the physical path mapped to by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the physical path
		///    mapped to by current instance.
		/// </value>
		public string PhysicalPath {
			get {
				if (rpath == null)
					rpath = GetParameter ("SCRIPT_FILENAME");
				
				return rpath;
			}
		}
		
		#endregion
		
		
		
		#region Parameter Handling
		
		/// <summary>
		///    This event is called when the parameter data has been
		///    completely read and parsed by the current instance.
		/// </summary>
		protected event EventHandler ParameterDataCompleted;
		
		/// <summary>
		///    Contains the paramter data as it is received from the
		///    server. Upon completion, the parameter data is parsed and
		///    the value is set to <see langword="null" />.
		/// </summary>
		#if NET_2_0
		List<byte> parameter_data = new List<byte> ();
		#else
		ArrayList parameter_data = new ArrayList ();
		#endif
		
		/// <summary>
		///    Contains the name/value pairs as they have been parsed.
		/// </summary>
		#if NET_2_0
		IDictionary<string,string> parameter_table = null;
		#else
		IDictionary parameter_table = null;
		#endif
		
		/// <summary>
		///    Adds a block of FastCGI parameter data to the current
		///    instance.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing a chunk of parameter
		///    data.
		/// </param>
		/// <remarks>
		///    <para>In the standard FastCGI method, if the data
		///    received has a length of zero, the parameter data has
		///    been completed and the the data will be parsed. At that
		///    point <see cref="ParameterDataCompleted" /> will be
		///    called.</para>
		///    <para>If an exception is encountered while parsing the
		///    parameters, the request will be aborted.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langref="null" />.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///    The parameter data has already been completed and parsed.
		/// </exception>
		public void AddParameterData (byte [] data)
		{
			// Validate arguments in public methods.
			if (data == null)
				throw new ArgumentNullException ("data");
			
			// When all the parameter data is received, it is acted
			// on and the parameter_data object is nullified.
			// Further data suggests a problem with the HTTP server.
			if (parameter_data == null) {
				Logger.Write (LogLevel.Warning,
					Strings.Request_ParametersAlreadyCompleted);
				return;
			}
			
			// If data was provided, append it to that already
			// received, and exit.
			if (data.Length > 0) {
				parameter_data.AddRange (data);
				return;
			}
			
			// A zero length record indicates the end of that form
			// of data. When it is received, the data can then be
			// examined and worked on.
			
			#if NET_2_0
			data = parameter_data.ToArray ();
			#else
			data = (byte []) parameter_data.ToArray (typeof (byte));
			#endif
			
			try {
				parameter_table = NameValuePair.FromData (data);
				// The parameter data is no longer needed and
				// can be sent to the garbage collector.
				parameter_data = null;
			
				// Inform listeners of the completion.
				if (ParameterDataCompleted != null)
					ParameterDataCompleted (this,
						EventArgs.Empty);
			} catch {
				Abort (Strings.Request_CanNotParseParameters);
			}

			ParseParameterData ();
		}

		/// <summary>
		///    Parses the parameters and tries to deduce SCRIPT_NAME & PATH_INFO
		///    from several other params supplied by the web server.
		///    Required by Apache.
		/// </summary>
		void ParseParameterData ()
		{
			string redirectUrl = GetParameter ("REDIRECT_URL");
			if (redirectUrl == null || redirectUrl.Length == 0)
				return;

			string pathInfo = GetParameter ("PATH_INFO");
			if (pathInfo == null || pathInfo.Length == 0)
				return;

			if (pathInfo [0] != '/' || pathInfo != redirectUrl)
				return;

			string pathTranslated = GetParameter ("PATH_TRANSLATED");
			if (pathTranslated == null || pathTranslated.Length == 0)
				return;

			string documentRoot = GetParameter ("DOCUMENT_ROOT");
			if (documentRoot == null || documentRoot.Length == 0)
				return;

			// At this point we have:
			//
			// REDIRECT_URL=/dir/test.aspx/foo
			// PATH_INFO=/dir/test.aspx/foo
			// PATH_TRANSLATED=/srv/www/htdocs/dir/test.aspx/foo
			// SCRIPT_NAME=/cgi-bin/fastcgi-mono-server
			// SCRIPT_FILENAME=/srv/www/cgi-bin/fastcgi-mono-server
			// DOCUMENT_ROOT=/srv/www/htdocs

			bool trailingSlash = pathTranslated [pathTranslated.Length - 1] == '/' ||
				(IOPath.DirectorySeparatorChar != '/' && pathTranslated [pathTranslated.Length - 1] == IOPath.DirectorySeparatorChar);

			if ((!trailingSlash && !File.Exists (pathTranslated)) || (trailingSlash && !Directory.Exists (pathTranslated))) {
				char [] separators;
				string physPath = pathTranslated;
				string filePath = null;

				if (IOPath.DirectorySeparatorChar == '/')
					separators = null;
				else
					separators = new char [] { '/', IOPath.DirectorySeparatorChar };

				// Reverse scan until the first existing file is found.
				// When the last existing component is a directory the next
				// component is considered to be the file name.

				while (true) {
					int index;
					string virtPath;
					string virtPathInfo;

					if (IOPath.DirectorySeparatorChar == '/')
						index = physPath.LastIndexOf ('/');
					else
						index = physPath.LastIndexOfAny (separators);

					// No more path components to trim
					if (index <= 0 || pathInfo.Length <= (pathTranslated.Length - index)) {
						if (filePath == null)
							break;

						physPath = filePath;
					} else {
						physPath = pathTranslated.Substring (0, index);

						if (!File.Exists (physPath)) {
							if (Directory.Exists (physPath)) {
								if (filePath == null)
									break;

								physPath = filePath;
							} else {
								filePath = physPath;
								continue;
							}
						}
					}

					// Now we set:
					//
					// SCRIPT_NAME=/dir/test.aspx
					// SCRIPT_FILENAME=/srv/www/htdocs/dir/test.aspx
					// PATH_INFO=/foo
					// PATH_TRANSLATED=/srv/www/htdocs/dir/foo

					virtPath = pathInfo.Substring (0, pathInfo.Length - (pathTranslated.Length - physPath.Length));
					virtPathInfo = pathInfo.Substring (virtPath.Length);

					// Ensure that physical and virtual path info are the same.
					if (IOPath.DirectorySeparatorChar == '/') {
						if (string.Compare (pathTranslated, physPath.Length, virtPathInfo, 0, virtPathInfo.Length) != 0)
							break;
					} else {
						if (pathTranslated.Substring (physPath.Length).Replace (IOPath.DirectorySeparatorChar, '/') != virtPathInfo)
							break;
					}

					SetParameter ("SCRIPT_NAME", virtPath);
					SetParameter ("SCRIPT_FILENAME", physPath);
					SetParameter ("PATH_INFO", virtPathInfo);
					// Actual physical path info may be different but this is safe and PHP does the same.
					if (documentRoot [documentRoot.Length - 1] == '/' ||
						(IOPath.DirectorySeparatorChar != '/' && documentRoot [documentRoot.Length - 1] == IOPath.DirectorySeparatorChar))
						documentRoot = documentRoot.Substring (0, documentRoot.Length - 1);
					SetParameter ("PATH_TRANSLATED", IOPath.GetFullPath (documentRoot + virtPathInfo));
					return;
				}
			}

			// There is no path info
			SetParameter ("SCRIPT_NAME", pathInfo);
			SetParameter ("SCRIPT_FILENAME", pathTranslated);
			SetParameter ("PATH_INFO", null);
			SetParameter ("PATH_TRANSLATED", null);
		}

		/// <summary>
		///    Gets a parameter with a specified name.
		/// </summary>
		/// <param name="parameter">
		///    A <see cref="string" /> containing a parameter namte to
		///    find in current instance.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> containing the parameter with the
		///    specified name, or <see langref="null" /> if it was not
		///    found.
		/// </returns>
		/// <remarks>
		///    This method is analogous to <see
		///    cref="System.Environment.GetEnvironmentVariable(string)"
		///    /> as FastCGI parameters represent environment variables
		///    that would be passed to a CGI/1.1 program.
		/// </remarks>
		public string GetParameter (string parameter)
		{
			#if NET_2_0
			if (parameter_table != null &&
				parameter_table.ContainsKey (parameter))
			#else
			if (parameter_table != null &&
				parameter_table.Contains (parameter))
			#endif
				return (string) parameter_table [parameter];
			
			return null;
		}

		void SetParameter (string name, string value)
		{
			if (parameter_table != null)
				parameter_table [name] = value;
		}
		
		/// <summary>
		///    Gets all parameter contained in the current instance.
		/// </summary>
		/// <returns>
		#if NET_2_0
		///    A <see cref="T:System.Collections.Generic.IDictionary&lt;string,string&gt;" />
		#else
		///    A <see cref="IDictionary" />
		#endif
		///    containing all the parameters contained in the current
		///    instance.
		/// </returns>
		/// <remarks>
		///    This method is analogous to <see
		///    cref="System.Environment.GetEnvironmentVariables()" /> as
		///    FastCGI parameters represent environment variables that
		///    would be passed to a CGI/1.1 program.
		/// </remarks>
		#if NET_2_0
		public IDictionary<string,string> GetParameters ()
		#else
		public IDictionary GetParameters ()
		#endif
		{
			return parameter_table;
		}
		
		#endregion
		
		
		
		#region Standard Input Handling
		
		/// <summary>
		///    This event is called when standard input data has been
		///    received by the current instance.
		/// </summary>
		/// <remarks>
		///    Input data is analogous to standard input in CGI/1.1
		///    programs and contains post data from the HTTP request.
		/// </remarks>
		protected event DataReceivedHandler  InputDataReceived;
		
		/// <summary>
		///    Indicates whether or not the standard input data has
		///    been completely read by the current instance.
		/// </summary>
		private bool input_data_completed = false;
		
		/// <summary>
		///    Adds a block of standard input data to the current
		///    instance.
		/// </summary>
		/// <param name="record">
		///    A <see cref="Record" /> containing a block of input
		///    data.
		/// </param>
		/// <remarks>
		///    <para>Input data is analogous to standard input in
		///    CGI/1.1 programs and contains post data from the HTTP
		///    request.</para>
		///    <para>When data is received, <see
		///    cref="InputDataReceived" /> is called.</para>
		/// </remarks>
		/// <exception cref="ArgumentException">
		///    <paramref name="record" /> does not have the type <see
		///    cref="RecordType.StandardInput" />.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///    The input data has already been completed.
		/// </exception>
		public void AddInputData (Record record)
		{
			// Validate arguments in public methods.
			if (record.Type != RecordType.StandardInput)
				throw new ArgumentException (
					Strings.Request_NotStandardInput,
					"record");
			
			// There should be no data following a zero byte record.
			if (input_data_completed) {
				Logger.Write (LogLevel.Warning,
					Strings.Request_StandardInputAlreadyCompleted);
				return;
			}

			if (record.BodyLength == 0)
				input_data_completed = true;
			
			// Inform listeners of the data.
			if (InputDataReceived != null)
				InputDataReceived (this, new DataReceivedArgs (record));
		}
		#endregion
		
		
		
		#region File Data Handling
		
		/// <summary>
		///    This event is called when file data has been received by
		///    the current instance.
		/// </summary>
		/// <remarks>
		///    File data send for the FastCGI filter role and contains
		///    the contents of the requested file to be filtered.
		/// </remarks>
		protected event DataReceivedHandler  FileDataReceived;
		
		/// <summary>
		///    Indicates whether or not the file data has been
		///    completely read by the current instance.
		/// </summary>
		private bool file_data_completed = false;
		
		/// <summary>
		///    Adds a block of file data to the current instance.
		/// </summary>
		/// <param name="record">
		///    A <see cref="Record" /> containing a block of file
		///    data.
		/// </param>
		/// <remarks>
		///    <para>File data send for the FastCGI filter role and
		///    contains the contents of the requested file to be
		///    filtered.</para>
		///    <para>When data is received, <see
		///    cref="FileDataReceived" /> is called.</para>
		/// </remarks>
		/// <exception cref="ArgumentException">
		///    <paramref name="record" /> does not have the type <see
		///    cref="RecordType.Data" />.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///    The file data has already been completed.
		/// </exception>
		public void AddFileData (Record record)
		{
			// Validate arguments in public methods.
			if (record.Type != RecordType.Data)
				throw new ArgumentException (
					Strings.Request_NotFileData,
					"record");
			
			// There should be no data following a zero byte record.
			if (file_data_completed) {
				Logger.Write (LogLevel.Warning,
					Strings.Request_FileDataAlreadyCompleted);
				return;
			}
			
			if (record.BodyLength == 0)
				file_data_completed = true;
			
			// Inform listeners of the data.
			if (FileDataReceived != null)
				FileDataReceived (this, new DataReceivedArgs (record));
		}
		#endregion
		
		
		
		#region Standard Output Handling
		
		/// <summary>
		///    Indicates whether or not output data has been sent.
		/// </summary>
		bool stdout_sent = false;
		
		/// <summary>
		///    Sends a specified number of bytes of standard output
		///    data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing output data to send.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> containing the number of bytes of
		///    <paramref name="data" /> to send.
		/// </param>
		/// <remarks>
		///    <para>FastCGI output data is analogous to CGI/1.1
		///    standard output data.</para>
		/// </remarks>
		public void SendOutput (byte [] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Length == 0)
				return;
			
			stdout_sent = true;
			
			SendStreamData (RecordType.StandardOutput, data, length);
		}
		
		/// <summary>
		///    Sends standard output data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing output data to send.
		/// </param>
		/// <remarks>
		///    <para>FastCGI output data is analogous to CGI/1.1
		///    standard output data.</para>
		///    <para>To send text, use <see
		///    cref="SendOutput(string,System.Text.Encoding)" />.</para>
		///    <para>To send only the beginning of a <see
		///    cref="byte[]" /> (as in the case of buffers), use <see
		///    cref="SendOutput(byte[],int)" />.</para>
		/// </remarks>
		public void SendOutput (byte [] data)
		{
			SendOutput (data, data.Length);
		}
		
		/// <summary>
		///    Sends standard outpu text in UTF-8 encoding.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> containing text to send.
		/// </param>
		/// <remarks>
		///    <para>FastCGI output data is analogous to CGI/1.1
		///    standard output data.</para>
		///    <para>To specify the text encoding, use <see
		///    cref="SendOutput(string,System.Text.Encoding)" />.</para>
		/// </remarks>
		public void SendOutputText (string text)
		{
			SendOutput (text, System.Text.Encoding.UTF8);
		}
		
		/// <summary>
		///    Sends standard output text in a specified encoding.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> containing text to send.
		/// </param>
		/// <param name="encoding">
		///    A <see cref="System.Text.Encoding" /> containing a
		///    encoding to use when converting the text.
		/// </param>
		/// <remarks>
		///    <para>FastCGI output data is analogous to CGI/1.1
		///    standard output data.</para>
		/// </remarks>
		public void SendOutput (string text, System.Text.Encoding encoding)
		{
			SendOutput (encoding.GetBytes (text));
		}
		#endregion
		
		
		
		#region Standard Error Handling
		
		/// <summary>
		///    Indicates whether or not error data has been sent.
		/// </summary>
		bool stderr_sent = false;
		
		/// <summary>
		///    Sends a specified number of bytes of standard error data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing error data to send.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> containing the number of bytes of
		///    <paramref name="data" /> to send.
		/// </param>
		/// <remarks>
		///    <para>FastCGI error data is analogous to CGI/1.1 standard
		///    error data.</para>
		/// </remarks>
		public void SendError (byte [] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Length == 0)
				return;
			
			stderr_sent = true;
			
			SendStreamData (RecordType.StandardError, data, length);
		}
		
		/// <summary>
		///    Sends standard error data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing error data to send.
		/// </param>
		/// <remarks>
		///    <para>FastCGI error data is analogous to CGI/1.1 standard
		///    error data.</para>
		///    <para>To send text, use <see
		///    cref="SendError(string,System.Text.Encoding)" />.</para>
		///    <para>To send only the beginning of a <see
		///    cref="byte[]" /> (as in the case of buffers), use <see
		///    cref="SendError(byte[],int)" />.</para>
		/// </remarks>
		public void SendError (byte [] data)
		{
			SendError (data, data.Length);
		}
		
		/// <summary>
		///    Sends standard error text in UTF-8 encoding.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> containing text to send.
		/// </param>
		/// <remarks>
		///    <para>FastCGI error data is analogous to CGI/1.1 standard
		///    error data.</para>
		///    <para>To specify the text encoding, use <see
		///    cref="SendError(string,System.Text.Encoding)" />.</para>
		/// </remarks>
		public void SendError (string text)
		{
			SendError (text, System.Text.Encoding.UTF8);
		}
		
		/// <summary>
		///    Sends standard error text in a specified encoding.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> containing text to send.
		/// </param>
		/// <param name="encoding">
		///    A <see cref="System.Text.Encoding" /> containing a
		///    encoding to use when converting the text.
		/// </param>
		/// <remarks>
		///    <para>FastCGI error data is analogous to CGI/1.1 standard
		///    error data.</para>
		/// </remarks>
		public void SendError (string text,
		                       System.Text.Encoding encoding)
		{
			SendError (encoding.GetBytes (text));
		}
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Sends a block of data with a specified length to the
		///    client in a specified type of record, splitting it into
		///    smaller records if the data is too large.
		/// </summary>
		/// <param name="type">
		///    A <see cref="RecordType" /> containing the type of
		///    record to send the data in.
		/// </param>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing the data to send.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> containing the length of the data to
		///    send. If greater than the length of <paramref
		///    name="data" />, it will decreased to that size.
		/// </param>
		private void SendStreamData (RecordType type, byte [] data,
		                             int length)
		{
			// Records are only able to hold 65535 bytes of data. If
			// larger data is to be sent, it must be broken into
			// smaller components.
			
			if (length > data.Length)
				length = data.Length;
			
			int max_size = 0x7fff;
			
			if (length < max_size)
				connection.SendRecord (type, requestID, data, 0,
					length);
			else
			{
				int index = 0;
				while (index < length)
				{
					int chunk_length = (max_size <
						length - index) ? max_size :
							(length - index);
					
					connection.SendRecord (type, requestID,
						data, index, chunk_length);
					
					index += chunk_length;
				}
			}
		}
		#endregion
	}
	
	/// <summary>
	///    This delegate is used for notification that data has been
	///    received, typically by <see cref="Request" />.
	/// </summary>
	/// <param name="sender">
	///    A <see cref="Request" /> object that sent the event.
	/// </param>
	/// <param name="args">
	///    A <see cref="DataReceivedArgs" /> object containing the arguments
	///    for the event.
	/// </param>
	public delegate void DataReceivedHandler (Request sender,
	                                          DataReceivedArgs args);
	
	/// <summary>
	///    This class extends <see cref="EventArgs" /> and provides
	///    arguments for the event that data is received.
	/// </summary>
	public class DataReceivedArgs : EventArgs
	{
		/// <summary>
		///    Contains the data that was received.
		/// </summary>
		private Record record;
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="DataReceivedArgs" /> with the specified data.
		/// </summary>
		/// <param name="record">
		///    A <see cref="Record" /> containing the data that was
		///    received.
		/// </param>
		public DataReceivedArgs (Record record)
		{
			this.record = record;
		}
		
		/// <summary>
		///    Gets whether or not the data has been completed.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not the data
		///    has been completed.
		/// </value>
		/// <remarks>
		///    Data completeness means that this is that last event
		///    of this type coming from the sender. It is the standard
		///    FastCGI test equivalent to <c><I>args</I>.Data.Length ==
		///    0</c>.
		/// </remarks>
		public bool DataCompleted {
			get {return record.BodyLength == 0;}
		}
		
		/// <summary>
		///    Gets the data that was received.
		/// </summary>
		/// <value>
		///    A <see cref="byte[]" /> containing the data that was
		///    received.
		/// </value>
		public byte[] GetData ()
		{
			return record.GetBody ();
		}
		
		/// <summary>
		///    Gets the length of the data in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> containing the length of the data
		///    in the current instance.
		/// </value>
		public int DataLength {
			get {return record.BodyLength;}
		}
		
		/// <summary>
		///    Copies the data to another array.
		/// </summary>
		/// <param name="dest">
		///    A <see cref="byte[]" /> to copy the body to.
		/// </param>
		/// <param name="destIndex">
		///    A <see cref="int" /> specifying at what index to start
		///    copying.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="dest" /> is <see langref="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="destIndex" /> is less than zero or does
		///    not provide enough space to copy the body.
		/// </exception>
		public void CopyTo (byte[] dest, int destIndex)
		{
			if (dest == null)
				throw new ArgumentNullException ("dest");
			
			if (DataLength > dest.Length - destIndex)
				throw new ArgumentOutOfRangeException (
					"destIndex");
			
			record.CopyTo (dest, destIndex);
		}
	}
}
