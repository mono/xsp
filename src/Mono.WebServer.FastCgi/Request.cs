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
using Mono.WebServer.FastCgi.Compatibility;
using Mono.WebServer.Log;
using IOPath = System.IO.Path;
using Mono.WebServer;
using Mono.WebServer.FastCgi;
using NRecord = Mono.WebServer.FastCgi.Record;
using System.Linq;

namespace Mono.FastCgi {
	public class Request
	{
		#region Private Fields
		
		readonly Connection connection;
		
		#endregion
		
		
		
		#region Constructors
		
		public Request (ushort requestID, Connection connection)
		{
			DataNeeded = true;

			RequestID  = requestID;
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
			
			connection.EndRequest (RequestID, appStatus,
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

		public bool DataNeeded { get; protected set; }
		
		public Server Server {
			get {return connection.Server;}
		}
		
		public ushort RequestID { get; private set; }
		
		public bool IsConnected {
			get {return connection.IsConnected;}
		}
		#endregion
		
		
		
		#region Request Details
		
		string vhost;
		
		int port = -1;
		
		string path;
		
		string rpath;

		public string HostName {
			get {
				if (String.IsNullOrEmpty(vhost))
					vhost = GetParameter ("HTTP_HOST");
				
				return vhost;
			}
		}
		
		public int PortNumber {
			get {
				if (port < 0)
					Int32.TryParse (GetParameter ("SERVER_PORT"),
						out port);

				return port;
			}
		}

		public string Path {
			get {
				if (String.IsNullOrEmpty(path))
					path = GetParameter ("SCRIPT_NAME");

				return path;
			}
		}
		
		public string PhysicalPath {
			get {
				if (String.IsNullOrEmpty(rpath))
					rpath = GetParameter ("SCRIPT_FILENAME");

				return rpath;
			}
		}
		
		internal protected ApplicationHost ApplicationHost { get; private set; }
		
		#endregion
		
		
		
		#region Parameter Handling
		
		protected event EventHandler ParameterDataCompleted;
		
		List<byte> parameter_data = new List<byte> ();
		
		IDictionary<string,string> parameter_table;
		
		public void AddParameterData (byte [] data)
		{
			AddParameterData (data.ToReadOnlyList ());
		}

		public void AddParameterData (IReadOnlyList<byte> data)
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
			if (data.Count > 0) {
				parameter_data.AddRange (data);
				return;
			}

			// A zero length record indicates the end of that form
			// of data. When it is received, the data can then be
			// examined and worked on.

			data = parameter_data.ToArray ().ToReadOnlyList ();
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
			VPathToHost vapp;
			if (String.IsNullOrEmpty(pathTranslated) || 
				String.IsNullOrEmpty(pathInfo) || pathInfo [0] != '/' || 
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
				vapp = WebServer.FastCgi.Server.GetApplicationForPath (HostName, PortNumber, Path, PhysicalPath);
				if (vapp == null)
					Logger.Write (LogLevel.Debug, "Couldn't find vapp.");
				else
					ApplicationHost = vapp.AppHost as ApplicationHost;
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
				vapp = WebServer.FastCgi.Server.GetApplicationForPath (
					HostName, PortNumber, virtPath, physPath);
				if (vapp == null)
					return;  // Set values in finally
				ApplicationHost = vapp.AppHost as ApplicationHost;
				if (ApplicationHost == null)
					return;  // Set values in finally

				// Split the virtual path and virtual path-info
				string verb = GetParameter ("REQUEST_METHOD");
				if (String.IsNullOrEmpty(verb))
					verb = "GET";  // For the sake of paths, assume a default
				ApplicationHost.GetPathsFromUri (verb, pathInfo, out virtPath, out virtPathInfo);
				if (virtPathInfo == null)
					virtPathInfo = String.Empty;
				if (virtPath == null)
					virtPath = String.Empty;

				// Re-map the physical path
				physPath = ApplicationHost.MapPath (virtPath) ?? String.Empty;

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
					if (ApplicationHost.VirtualFileExists (virtPath)) {
						physRoot = IOPath.GetDirectoryName (physRoot) ?? String.Empty;
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
				return parameter_table [parameter];
			
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
		
		bool input_data_completed;

		[Obsolete]
		public void AddInputData (Record record)
		{
			AddInputData ((NRecord)record);
		}

		internal void AddInputData (NRecord record)
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
		
		protected event DataReceivedHandler FileDataReceived;
		
		bool file_data_completed;

		[Obsolete]
		public void AddFileData (Record record)
		{
			AddFileData ((NRecord)record);
		}

		internal void AddFileData (NRecord record)
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
		
		bool stdout_sent;
		
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
		
		bool stderr_sent;
		
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
		
		void SendStreamData (RecordType type, byte [] data,
		                     int length)
		{
			// Records are only able to hold 65535 bytes of data. If
			// larger data is to be sent, it must be broken into
			// smaller components.
			
			if (length > data.Length)
				length = data.Length;
			
			const int maxSize = 0x7fff;
			
			if (length < maxSize)
				connection.SendRecord (type, RequestID, data, 0,
					length);
			else
			{
				int index = 0;
				while (index < length)
				{
					int chunkLength = (maxSize <
						length - index) ? maxSize :
							(length - index);
					
					connection.SendRecord (type, RequestID,
						data, index, chunkLength);
					
					index += chunkLength;
				}
			}
		}
		#endregion
	}
	
	public delegate void DataReceivedHandler (Request sender,
	                                          DataReceivedArgs args);
	
	public class DataReceivedArgs : EventArgs
	{
		readonly NRecord record;

		[Obsolete]
		public DataReceivedArgs (Record record)
		{
			this.record = (NRecord)record;
		}

		internal DataReceivedArgs(NRecord record)
		{
			this.record = record;
		}
		
		public bool DataCompleted {
			get {return record.BodyLength == 0;}
		}

		[Obsolete("Use GetBody()")]
		public byte[] GetData ()
		{
			return record.GetBody ().ToArray ();
		}

		public IReadOnlyList<byte> GetBody()
		{
			return record.GetBody();
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
