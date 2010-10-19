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
using System.Collections.Generic;
using System.IO;
using System.Text;
using IOPath = System.IO.Path;

namespace Mono.FastCgi {
	public class Request
	{
		#region Private Fields
		
		private ushort requestID;
		
		private Connection connection;
		
		#endregion
		
		
		
		#region Constructors
		
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
		
		public void Abort (string message, params object [] args)
		{
			SendError (message);
			Logger.Write (LogLevel.Error,
				Strings.Request_Aborting, RequestID);
			Logger.Write (LogLevel.Error, message, args);
			CompleteRequest (-1, ProtocolStatus.RequestComplete);
		}
		
		private bool data_needed = true;
		
		public bool DataNeeded {
			get {return data_needed;}
			protected set {data_needed = value;}
		}
		
		public Server Server {
			get {return connection.Server;}
		}
		
		public ushort RequestID {
			get {return requestID;}
		}
		
		public bool IsConnected {
			get {return connection.IsConnected;}
		}
		#endregion
		
		
		
		#region Request Details
		
		private string vhost = null;
		
		private int port = -1;
		
		private string path = null;
		
		private string rpath = null;
		
		private Mono.WebServer.FastCgi.ApplicationHost appHost;
		
		public string HostName {
			get {
				if (vhost == null)
					vhost = GetParameter ("HTTP_HOST");
				
				return vhost;
			}
		}
		
		public int PortNumber {
			get {
				if (port < 0)
					port = int.Parse (GetParameter (
							"SERVER_PORT"));
				
				return port;
			}
		}
		
		public string Path {
			get {
				if (path == null)
					path = GetParameter ("SCRIPT_NAME");
				
				return path;
			}
		}
		
		public string PhysicalPath {
			get {
				if (rpath == null)
					rpath = GetParameter ("SCRIPT_FILENAME");
				
				return rpath;
			}
		}
		
		internal protected Mono.WebServer.FastCgi.ApplicationHost ApplicationHost {
			get {
				return appHost;
			}
		}
		
		#endregion
		
		
		
		#region Parameter Handling
		
		protected event EventHandler ParameterDataCompleted;
		
		List<byte> parameter_data = new List<byte> ();
		
		IDictionary<string,string> parameter_table = null;
		
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
			
			data = parameter_data.ToArray ();			
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

		void ParseParameterData ()
		{
			string redirectUrl;
			string pathInfo = GetParameter ("PATH_INFO");
			string pathTranslated = GetParameter ("PATH_TRANSLATED");
			Mono.WebServer.VPathToHost vapp;
			if (pathTranslated == null || pathTranslated.Length == 0 || 
				pathInfo == null || pathInfo.Length == 0 || pathInfo [0] != '/' || 
				(null != (redirectUrl = GetParameter ("REDIRECT_URL")) && redirectUrl.Length != 0 && pathInfo != redirectUrl)) {
				// Only consider REDIRECT_URL if it actually contains 
				// something, since it may not always be present (depending 
				// on installed Apache modules & setup).  Also, never allow 
				// PATH_INFO to be null (nor PATH_TRANSLATED), even for 
				// cases where this method is mostly short-circuited.
				if (pathInfo == null)
					SetParameter ("PATH_INFO", String.Empty);
				if (pathTranslated == null)
					SetParameter ("PATH_TRANSLATED", String.Empty);
				vapp = Mono.WebServer.FastCgi.Server.GetApplicationForPath (this.HostName, this.PortNumber, this.Path, this.PhysicalPath);
				if (vapp != null)
					appHost = (Mono.WebServer.FastCgi.ApplicationHost)vapp.AppHost;
				return;
			}

			// At this point we have:  (with REDIRECT_URL being optional)
			//
			// REDIRECT_URL=/dir/test.aspx/foo
			// PATH_INFO=/dir/test.aspx/foo
			// PATH_TRANSLATED=/srv/www/htdocs/dir/test.aspx/foo
			// SCRIPT_NAME=/cgi-bin/fastcgi-mono-server
			// SCRIPT_FILENAME=/srv/www/cgi-bin/fastcgi-mono-server

			string virtPath = pathInfo;
			string physPath = pathTranslated;
			string virtPathInfo = String.Empty;
			string physPathInfo = String.Empty;
			try {
				vapp = Mono.WebServer.FastCgi.Server.GetApplicationForPath (
					this.HostName, this.PortNumber, virtPath, physPath);
				if (vapp == null)
					return;  // Set values in finally
				appHost = (Mono.WebServer.FastCgi.ApplicationHost)vapp.AppHost;
				if (appHost == null)
					return;  // Set values in finally

				// Split the virtual path and virtual path-info
				string verb = GetParameter ("REQUEST_METHOD");
				if (verb == null || verb.Length == 0)
					verb = "GET";  // For the sake of paths, assume a default
				appHost.GetPathsFromUri (verb, pathInfo, out virtPath, out virtPathInfo);
				if (virtPathInfo == null)
					virtPathInfo = String.Empty;
				if (virtPath == null)
					virtPath = String.Empty;

				// Re-map the physical path
				physPath = appHost.MapPath (virtPath);
				if (physPath == null)
					physPath = String.Empty;

				// Re-map the physical path-info
				string relaPathInfo = virtPathInfo;
				if (relaPathInfo.Length != 0 && IOPath.DirectorySeparatorChar != '/')
					relaPathInfo = relaPathInfo.Replace ('/', IOPath.DirectorySeparatorChar);
				while (relaPathInfo.Length > 0 && relaPathInfo[0] == IOPath.DirectorySeparatorChar) {
					relaPathInfo = relaPathInfo.Substring (1);
				}
				if (physPath.Length == 0) {
					physPathInfo = relaPathInfo;
					return;  // Set values in finally
				}
				string physRoot = physPath;
				try {
					if (appHost.VirtualFileExists (virtPath)) {
						physRoot = IOPath.GetDirectoryName (physRoot);
						if (physRoot == null)
							physRoot = String.Empty;
					}
				} catch {
					// Assume virtPath, physPath & physRoot 
					// specify directories (and not files)
				}
				physPathInfo = IOPath.Combine (physRoot, relaPathInfo);
			} finally {
				// Now, if all went well, we set:
				//
				// SCRIPT_NAME=/dir/test.aspx
				// SCRIPT_FILENAME=/srv/www/htdocs/dir/test.aspx
				// PATH_INFO=/foo
				// PATH_TRANSLATED=/srv/www/htdocs/dir/foo

				SetParameter ("SCRIPT_NAME", virtPath);
				SetParameter ("SCRIPT_FILENAME", physPath);
				SetParameter ("PATH_INFO", virtPathInfo);
				SetParameter ("PATH_TRANSLATED", physPathInfo);
			}
		}

		public string GetParameter (string parameter)
		{
			if (parameter_table != null && parameter_table.ContainsKey (parameter))
				return (string) parameter_table [parameter];
			
			return null;
		}

		void SetParameter (string name, string value)
		{
			if (parameter_table != null)
				parameter_table [name] = value;
		}
		
		public IDictionary<string,string> GetParameters ()
		{
			return parameter_table;
		}
		
		#endregion
		
		
		
		#region Standard Input Handling
		
		protected event DataReceivedHandler  InputDataReceived;
		
		private bool input_data_completed = false;
		
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
		
		protected event DataReceivedHandler  FileDataReceived;
		
		private bool file_data_completed = false;
		
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
		
		bool stdout_sent = false;
		
		public void SendOutput (byte [] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Length == 0)
				return;
			
			stdout_sent = true;
			
			SendStreamData (RecordType.StandardOutput, data, length);
		}
		
		public void SendOutput (byte [] data)
		{
			SendOutput (data, data.Length);
		}
		
		public void SendOutputText (string text)
		{
			SendOutput (text, System.Text.Encoding.UTF8);
		}
		
		public void SendOutput (string text, System.Text.Encoding encoding)
		{
			SendOutput (encoding.GetBytes (text));
		}
		#endregion
		
		
		
		#region Standard Error Handling
		
		bool stderr_sent = false;
		
		public void SendError (byte [] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Length == 0)
				return;
			
			stderr_sent = true;
			
			SendStreamData (RecordType.StandardError, data, length);
		}
		
		public void SendError (byte [] data)
		{
			SendError (data, data.Length);
		}
		
		public void SendError (string text)
		{
			SendError (text, System.Text.Encoding.UTF8);
		}
		
		public void SendError (string text,
		                       System.Text.Encoding encoding)
		{
			SendError (encoding.GetBytes (text));
		}
		#endregion
		
		
		
		#region Private Methods
		
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
	
	public delegate void DataReceivedHandler (Request sender,
	                                          DataReceivedArgs args);
	
	public class DataReceivedArgs : EventArgs
	{
		private Record record;
		
		public DataReceivedArgs (Record record)
		{
			this.record = record;
		}
		
		public bool DataCompleted {
			get {return record.BodyLength == 0;}
		}
		
		public byte[] GetData ()
		{
			return record.GetBody ();
		}
		
		public int DataLength {
			get {return record.BodyLength;}
		}
		
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
