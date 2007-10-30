//
// Connection.cs: Handles a FastCGI connection.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
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

namespace Mono.FastCgi {
	/// <summary>
	///    This class handles a FastCGI connection by processing records and
	///    calling responders.
	/// </summary>
	public class Connection
	{
		#region Private Fields
		
		/// <summary>
		///    Contains a list of the current requests.
		/// </summary>
		#if NET_2_0
		private List<Request> requests = new List<Request> ();
		#else
		private ArrayList requests = new ArrayList ();
		#endif
		
		/// <summary>
		///    Contains the socket to communicate over.
		/// </summary>
		private Socket socket;
		
		/// <summary>
		///    Contains the server that created the current instance.
		/// </summary>
		private Server server;
		
		/// <summary>
		///    Indicates whether or not to keep the connection alive
		///    after a current requests has been completed.
		/// </summary>
		private bool keep_alive;
		
		/// <summary>
		///    Indicates whether or not to stop the current connection.
		/// </summary>
		private bool stop;
		
		/// <summary>
		///    Contains the lock used to regulate the modifying of the
		///    request list.
		/// </summary>
		private object request_lock = new object ();
		
		/// <summary>
		///    Contains the lock used to regulate the sending of
		///    records.
		/// </summary>
		private object send_lock = new object ();
		
		/// <summary>
		///    Contains the buffer used to receive records.
		/// </summary>
		private byte[] receive_buffer;
		
		/// <summary>
		///    Contains the buffer used to send records.
		/// </summary>
		private byte[] send_buffer;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Connection" /> to handle a specified socket created
		///    by a specified server.
		/// </summary>
		/// <param name="socket">
		///    A <see cref="Socket" /> object to communicate over.
		/// </param>
		/// <param name="server">
		///    A <see cref="Server" /> object containing the server that
		///    created the new instance.
		/// </param>
		public Connection (Socket socket, Server server)
		{
			this.socket = socket;
			this.server = server;
			server.AllocateBuffers (out receive_buffer,
				out send_buffer);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the number of active requests being managed by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the number of active
		///    requests being managed by the current instance.
		/// </value>
		public int RequestCount {
			get {return requests.Count;}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is connected.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not the
		///    current instance is connected.
		/// </value>
		public bool IsConnected {
			get {return socket.Connected;}
		}
		
		/// <summary>
		///    Gets the server used to create the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="Server" /> object containing the server used
		///    to create the current instance.
		/// </value>
		public Server Server {
			get {return server;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Receives and responds to records until all requests have
		///    been completed.
		/// </summary>
		/// <remarks>
		///    If the last received BeginRequest record is flagged
		///    with keep-alive, the connection will be kept alive event
		///    after all open requests have been completed.
		/// </remarks>
		public void Run ()
		{
			Logger.Write (LogLevel.Notice,
				Strings.Connection_BeginningRun);
			
			do {
				Record record;
				
				try {
					record = new Record (socket,
						receive_buffer);
				} catch (System.Net.Sockets.SocketException) {
					StopRun (Strings.Connection_RecordNotReceived);
					Stop ();
					break;
				}
				
				Request request = GetRequest (record.RequestID);
				
				switch (record.Type) {
					
				// Creates a new request.
				case RecordType.BeginRequest:
					
					// If a request with the given ID
					// already exists, there's a bug in the
					// client. Abort.
					if (request != null) {
						StopRun (Strings.Connection_RequestAlreadyExists);
						break;
					}
					
					// If there are unfinished requests
					// and multiplexing is disabled, inform
					// the client and don't begin the
					// request.
					if (!server.MultiplexConnections &&
						UnfinishedRequests) {
						EndRequest (record.RequestID, 0,
							ProtocolStatus.CantMultiplexConnections);
						break;
					}
					
					// If the maximum number of requests is
					// reached, inform the client and don't
					// begin the request.
					if (!server.CanRequest) {
						EndRequest (record.RequestID, 0,
							ProtocolStatus.Overloaded);
						break;
					}
					
					BeginRequestBody body = new BeginRequestBody
						(record);
						
					// If the role is "Responder", and it is
					// supported, create a ResponderRequest.
					if (body.Role == Role.Responder &&
						server.SupportsResponder)
						request = new ResponderRequest
							(record.RequestID, this);
						
					// If the request is null, the role is
					// not supported. Inform the client and
					// don't begin the request.
					if (request == null) {
						Logger.Write (LogLevel.Warning,
							Strings.Connection_RoleNotSupported,
							body.Role);
						EndRequest (record.RequestID, 0,
							ProtocolStatus.UnknownRole);
						break;
					}
					
					lock (request_lock) {
						requests.Add (request);
					}
					
					keep_alive = (body.Flags &
						BeginRequestFlags.KeepAlive) != 0;
					
				break;
				
				// Gets server values.
				case RecordType.GetValues:
					byte [] response_data;
					
					// Look up the data from the server.
					try {
						#if NET_2_0
						IDictionary<string,string> pairs_in  =
							NameValuePair.FromData (
								record.GetBody ());
						IDictionary<string,string> pairs_out = 
							server.GetValues (
								pairs_in.Keys);
						#else
						IDictionary pairs_in  =
							NameValuePair.FromData (
								record.GetBody ());
						IDictionary pairs_out =
							server.GetValues (
								pairs_in.Keys);
						#endif
						response_data = NameValuePair.GetData (pairs_out);
					} catch {
						response_data = new byte [0];
					}
					
					SendRecord (RecordType.GetValuesResult,
						record.RequestID, response_data);
				break;
				
				// Sends params to the request.
				case RecordType.Params:
					if (request == null) {
						StopRun (Strings.Connection_RequestDoesNotExist,
							record.RequestID);
						break;
					}
					
					request.AddParameterData (record.GetBody ());
				
				break;
					
				// Sends standard input to the request.
				case RecordType.StandardInput:
					if (request == null) {
						StopRun (Strings.Connection_RequestDoesNotExist,
							record.RequestID);
					}
					
					request.AddInputData (record);
				
				break;
				
				// Sends file data to the request.
				case RecordType.Data:
					if (request == null) {
						StopRun (Strings.Connection_RequestDoesNotExist,
							record.RequestID);
					}
					
					request.AddFileData (record);
				
				break;
				
				// Aborts a request when the server aborts.
				case RecordType.AbortRequest:
					if (request != null)
						break;
					
					request.Abort (
						Strings.Connection_AbortRecordReceived);
				
				break;
				
				// Informs the client that the record type is
				// unknown.
				default:
					Logger.Write (LogLevel.Warning,
						Strings.Connection_UnknownRecordType,
						record.Type);
					SendRecord (RecordType.UnknownType,
						record.RequestID,
						new UnknownTypeBody (
							record.Type).GetData ());
				
				break;
				}
			}
			while (!stop && (UnfinishedRequests || keep_alive));
			
			if (requests.Count == 0) {
				socket.Close ();
				server.EndConnection (this);
				server.ReleaseBuffers (receive_buffer,
					send_buffer);
			}
			
			Logger.Write (LogLevel.Notice,
				Strings.Connection_EndingRun);
		}
		
		/// <summary>
		///    Sends a record to the client.
		/// </summary>
		/// <param name="type">
		///    A <see cref="RecordType" /> specifying the type of record
		///    to send.
		/// </param>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    the record is associated with.
		/// </param>
		/// <param name="bodyData">
		///    A <see cref="byte[]" /> containing the body data for the
		///    request.
		/// </param>
		/// <remarks>
		///    If the socket is not connected, the record will not be
		///    sent.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="bodyData" /> is <see langword="null" />.
		/// </exception>
		public void SendRecord (RecordType type, ushort requestID,
		                        byte [] bodyData)
		{
			SendRecord (type, requestID, bodyData, 0, -1);
		}
		
		/// <summary>
		///    Sends a record to the client.
		/// </summary>
		/// <param name="type">
		///    A <see cref="RecordType" /> specifying the type of record
		///    to send.
		/// </param>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    the record is associated with.
		/// </param>
		/// <param name="bodyData">
		///    A <see cref="byte[]" /> containing the body data for the
		///    request.
		/// </param>
		/// <param name="bodyIndex">
		///    A <see cref="int" /> specifying the index in <paramref
		///    name="bodyData" /> at which the body begins.
		/// </param>
		/// <param name="bodyLength">
		///    A <see cref="int" /> specifying the length of the body in
		///    <paramref name="bodyData" /> or -1 if all remaining data
		///    (<c><paramref name="bodyData" />.Length - <paramref
		///    name="bodyIndex" /></c>) is used.
		/// </param>
		/// <remarks>
		///    If the socket is not connected, the record will not be
		///    sent.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="bodyData" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="bodyIndex" /> is outside of the range
		///    of <paramref name="bodyData" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="bodyLength" /> contains more than 65535
		///    bytes or is set to -1 and calculated to be greater than
		///    65535 bytes.
		/// </exception>
		public void SendRecord (RecordType type, ushort requestID,
		                        byte [] bodyData, int bodyIndex,
		                        int bodyLength)
		{
			if (IsConnected)
				lock (send_lock) {
					try {
						new Record (1, type, requestID,
						bodyData, bodyIndex,
							bodyLength).Send (
								socket,
								send_buffer);
					} catch (System.Net.Sockets.SocketException) {
					}
				}
		}
		
		/// <summary>
		///    Sends an EndRequest record with a specified request ID,
		///    application status, and protocol status, and releases the
		///    associated resources.
		/// </summary>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    to end.
		/// </param>
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
		public void EndRequest (ushort requestID, int appStatus,
		                        ProtocolStatus protocolStatus)
		{
			EndRequestBody body = new EndRequestBody (appStatus,
				protocolStatus);
			
			if (IsConnected)
				new Record (1, RecordType.EndRequest, requestID,
					body.GetData ()).Send (socket);
			
			int index = GetRequestIndex (requestID);
			
			if (index >= 0) {
				lock (request_lock) {
					requests.RemoveAt (index);
				}
			}
			
			if (requests.Count == 0 && (!keep_alive || stop)) {
				socket.Close ();
				server.EndConnection (this);
				server.ReleaseBuffers (receive_buffer,
					send_buffer);
			}
		}
		
		/// <summary>
		///    Stops the current instance by ending all the open
		///    requests and closing the socket.
		/// </summary>
		public void Stop ()
		{
			stop = true;
			
			#if NET_2_0
			foreach (Request req in new List<Request> (requests))
			#else
			foreach (Request req in new ArrayList (requests))
			#endif
				EndRequest (req.RequestID, -1,
					ProtocolStatus.RequestComplete);
		}
		
		#endregion
		
		
		
		#region Private Properties
		
		/// <summary>
		///    Gets whether or not more request data is expected by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not more
		///    request data is expected by the current instance.
		/// </value>
		/// <remarks>
		///    If <see langword="true" />, more data is expected from a
		///    open request. For instance, the input data has not been
		///    completed.
		/// </remarks>
		private bool UnfinishedRequests {
			get {
				foreach (Request request in requests)
					if (request.DataNeeded)
						return true;
				
				return false;
			}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Gets the request in the current instance with a specified
		///    request ID.
		/// </summary>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    to get.
		/// </param>
		/// <returns>
		///    A <see cref="Request" /> object containing the request
		///    with the with the specified request ID, or <see
		///    langword="null" /> if it was not found.
		/// </returns>
		private Request GetRequest (ushort requestID)
		{
			foreach (Request request in requests)
				if (request.RequestID == requestID)
					return request;
			
			return null;
		}
		
		/// <summary>
		///    Gets the index of the request in <see cref="requests" />
		///    with a specified request ID.
		/// </summary>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    to get.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> containing the index of the request
		///    in <see cref="requests" /> with the with the specified
		///    request ID, or -1 if it was not found.
		/// </returns>
		private int GetRequestIndex (ushort requestID)
		{
			int i = 0;
			int count = requests.Count;
			while (i < count &&
				(requests [i] as Request).RequestID != requestID)
				i ++;
			
			return (i != count) ? i : -1;
		}
		
		/// <summary>
		///    Flags the process to stop and writes an error message to
		///    the log.
		/// </summary>
		private void StopRun (string message, params object [] args)
		{
			Logger.Write (LogLevel.Error, message, args);
			Logger.Write (LogLevel.Error,
				Strings.Connection_Terminating);
			
			Stop ();
		}
		
		#endregion
	}
}
