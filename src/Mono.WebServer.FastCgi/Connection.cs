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
using System.Collections.Generic;

namespace Mono.FastCgi {
	public class Connection
	{
		#region Private Fields
		
		private List<Request> requests = new List<Request> ();
		
		private Socket socket;
		
		private Server server;
		
		private bool keep_alive;
		
		private bool stop;
		
		private object request_lock = new object ();
		
		private object send_lock = new object ();
		
		private byte[] receive_buffer;
		
		private byte[] send_buffer;

		object connection_teardown_lock = new object ();
		#endregion
		
		
		
		#region Constructors
		
		public Connection (Socket socket, Server server)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			if (server == null)
				throw new ArgumentNullException ("server");
			
			this.socket = socket;
			this.server = server;
			server.AllocateBuffers (out receive_buffer, out send_buffer);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		public int RequestCount {
			get {return requests.Count;}
		}
		
		public bool IsConnected {
			get {
				if (socket == null)
					return false;
				return socket.Connected;
			}
		}
		
		public Server Server {
			get {return server;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		public void Run ()
		{
			Logger.Write (LogLevel.Notice,
				Strings.Connection_BeginningRun);
			if (socket == null) {
				Logger.Write (LogLevel.Notice, Strings.Connection_NoSocketInRun);
				return;
			}
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
						IDictionary<string,string> pairs_in = NameValuePair.FromData (record.GetBody ());
						IDictionary<string,string> pairs_out = server.GetValues (pairs_in.Keys);
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
						break;
					}
					
					request.AddInputData (record);
				
				break;
				
				// Sends file data to the request.
				case RecordType.Data:
					if (request == null) {
						StopRun (Strings.Connection_RequestDoesNotExist,
							record.RequestID);
						break;
					}
					
					request.AddFileData (record);
				
				break;
				
				// Aborts a request when the server aborts.
				case RecordType.AbortRequest:
					if (request == null)
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
				lock (connection_teardown_lock) {
					try {
						if (socket != null)
							socket.Close ();
					} catch (System.Net.Sockets.SocketException e) {
						// Ignore: "The descriptor is not a socket"
						//         error from UnmanagedSocket.Close
						if (e.ErrorCode != 10038)
							throw;  // Rethrow other errors
					} finally {
						socket = null;
					}
					if (!stop)
						server.EndConnection (this);
					if (receive_buffer != null && send_buffer != null) {
						server.ReleaseBuffers (receive_buffer, send_buffer);
						receive_buffer = null;
						send_buffer = null;
					}
				}
			}
			
			Logger.Write (LogLevel.Notice,
				Strings.Connection_EndingRun);
		}
		
		public void SendRecord (RecordType type, ushort requestID,
		                        byte [] bodyData)
		{
			SendRecord (type, requestID, bodyData, 0, -1);
		}
		
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
		
		public void EndRequest (ushort requestID, int appStatus,
		                        ProtocolStatus protocolStatus)
		{
			EndRequestBody body = new EndRequestBody (appStatus,
				protocolStatus);
			try {	
				if (IsConnected)
					new Record (1, RecordType.EndRequest, requestID,
						    body.GetData ()).Send (socket);
			} catch (System.Net.Sockets.SocketException) {
			}
				
			
			int index = GetRequestIndex (requestID);
			
			if (index >= 0) {
				lock (request_lock) {
					requests.RemoveAt (index);
				}
			}

			lock (connection_teardown_lock) {
				if (requests.Count == 0 && (!keep_alive || stop)) {
					if (socket != null) {
						try {
							socket.Close ();
						} finally {
							socket = null;
						}
					}

					if (!stop)
						server.EndConnection (this);
					if (receive_buffer != null && send_buffer != null) {
						server.ReleaseBuffers (receive_buffer, send_buffer);
						receive_buffer = null;
						send_buffer = null;
					}
				}
			}
		}
		
		public void Stop ()
		{
			stop = true;
			
			foreach (Request req in new List<Request> (requests))
				EndRequest (req.RequestID, -1, ProtocolStatus.RequestComplete);
		}
		
		#endregion
		
		
		
		#region Private Properties
		
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
		
		private Request GetRequest (ushort requestID)
		{
			foreach (Request request in requests)
				if (request.RequestID == requestID)
					return request;
			
			return null;
		}
		
		private int GetRequestIndex (ushort requestID)
		{
			int i = 0;
			int count = requests.Count;
			while (i < count &&
				(requests [i] as Request).RequestID != requestID)
				i ++;
			
			return (i != count) ? i : -1;
		}
		
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
