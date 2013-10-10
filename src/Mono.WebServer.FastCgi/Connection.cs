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
using Mono.WebServer.Log;
using Mono.WebServer.FastCgi;
using NRecord = Mono.WebServer.FastCgi.Record;
using Mono.WebServer.FastCgi.Compatibility;

namespace Mono.FastCgi {
	public class Connection
	{
		#region Private Fields
		
		readonly List<Request> requests = new List<Request> ();
		
		Socket socket;

		readonly Server server;
		
		bool keep_alive;
		
		bool stop;

		readonly object request_lock = new object ();

		readonly object send_lock = new object ();

		readonly Buffers receive_buffers;

		readonly Buffers send_buffers;

		readonly object connection_teardown_lock = new object ();
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

			receive_buffers = new Buffers(server.BigBufferManager, server.SmallBufferManager);
			send_buffers = new Buffers (server.BigBufferManager, server.SmallBufferManager);
		}
		
		#endregion
		
		
		
		#region Public Properties

		public event EventHandler RequestReceived;
		
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
			Logger.Write (LogLevel.Debug, Strings.Connection_BeginningRun);
			if (socket == null) {
				Logger.Write (LogLevel.Notice, Strings.Connection_NoSocketInRun);
				return;
			}
			do {
				NRecord record;
				
				try {
					record = NRecord.Receive(socket, receive_buffers);
				} catch (System.Net.Sockets.SocketException) {
					StopRun (Strings.Connection_RecordNotReceived);
					Stop ();
					break;
				}
				
				Request request = GetRequest (record.RequestID);

				try {
					if (RequestReceived != null)
						RequestReceived.BeginInvoke (this, EventArgs.Empty, null, null);
				} catch(Exception e) {
					// We don't care if the event handler has problems
					Logger.Write(LogLevel.Error, "Error while invoking RequestReceived event:");
					Logger.Write(e);
				}

				Logger.Write(LogLevel.Debug, "Now handling record (with type {0})", record.Type);
				
				HandleRequest (record, request);
			}
			while (!stop && (UnfinishedRequests || keep_alive));

			if (requests.Count == 0) {
				lock (connection_teardown_lock) {
					CloseSocket();

					if (!stop)
						server.EndConnection (this);

					send_buffers.Return ();
					receive_buffers.Return ();
				}
			}
			
			Logger.Write (LogLevel.Debug, Strings.Connection_EndingRun);
		}

		void CloseSocket ()
		{
			try {
				if (socket != null)
					socket.Close ();
			} catch (System.Net.Sockets.SocketException e) {
				// Ignore: "The descriptor is not a socket"
				//         error from UnmanagedSocket.Close
				if (e.ErrorCode != 10038)
					throw;  // Rethrow other errors
			} catch (ObjectDisposedException){
				// Ignore: already closed
			} finally {
				socket = null;
			}
		}

		void HandleRequest (NRecord record, Request request)
		{
			switch (record.Type) {
				// Creates a new request.
			case RecordType.BeginRequest:
				HandleBeginRequest (request, record);
				break;

				// Gets server values.
			case RecordType.GetValues:
				HandleGetValues (record);
				break;

				// Sends params to the request.
			case RecordType.Params:
				HandleParams (request, record);
				break;

				// Sends standard input to the request.
			case RecordType.StandardInput:
				HandleStandardInput (request, record);
				break;

				// Sends file data to the request.
			case RecordType.Data:
				HandleData (request, record);
				break;

				// Aborts a request when the server aborts.
			case RecordType.AbortRequest:
				HandleAbortRequest (request);
				break;

				// Informs the client that the record type is
				// unknown.
			default:
				HandleUnknown (record);
				break;
			}
		}

		void HandleUnknown (NRecord record)
		{
			Logger.Write (LogLevel.Warning, Strings.Connection_UnknownRecordType, record.Type);
			SendRecord (RecordType.UnknownType, record.RequestID, new UnknownTypeBody (record.Type).GetData ());
		}

		static void HandleAbortRequest (Request request)
		{
			if (request == null)
				return;

			request.Abort (Strings.Connection_AbortRecordReceived);
		}

		void HandleData (Request request, NRecord record)
		{
			if (request == null) {
				StopRun (Strings.Connection_RequestDoesNotExist, record.RequestID);
				return;
			}

			request.AddFileData (record);
		}

		void HandleStandardInput (Request request, NRecord record)
		{
			if (request == null) {
				StopRun (Strings.Connection_RequestDoesNotExist, record.RequestID);
				return;
			}

			request.AddInputData (record);
		}

		void HandleParams (Request request, NRecord record)
		{
			if (request == null) {
				StopRun (Strings.Connection_RequestDoesNotExist, record.RequestID);
				return;
			}

			IReadOnlyList<byte> body = record.GetBody ();
			request.AddParameterData (body);
		}

		void HandleGetValues (NRecord record)
		{
			byte[] response_data;

			// Look up the data from the server.
			try {
				IReadOnlyList<byte> body = record.GetBody ();
				IDictionary<string, string> pairs_in = NameValuePair.FromData (body);
				IDictionary<string, string> pairs_out = server.GetValues (pairs_in.Keys);
				response_data = NameValuePair.GetData (pairs_out);
			} catch {
				response_data = new byte[0];
			}

			SendRecord (RecordType.GetValuesResult, record.RequestID, response_data);
		}

		void HandleBeginRequest (Request request, NRecord record)
		{
			// If a request with the given ID
			// already exists, there's a bug in the
			// client. Abort.
			if (request != null) {
				StopRun (Strings.Connection_RequestAlreadyExists);
				return;
			}

			// If there are unfinished requests
			// and multiplexing is disabled, inform
			// the client and don't begin the
			// request.
			if (!server.MultiplexConnections && UnfinishedRequests) {
				EndRequest (record.RequestID, 0, ProtocolStatus.CantMultiplexConnections);
				return;
			}

			// If the maximum number of requests is
			// reached, inform the client and don't
			// begin the request.
			if (!server.CanRequest) {
				EndRequest (record.RequestID, 0, ProtocolStatus.Overloaded);
				return;
			}

			var body = new BeginRequestBody (record);

			// If the role is "Responder", and it is
			// supported, create a ResponderRequest.
			if (body.Role == Role.Responder && server.SupportsResponder)
				request = new ResponderRequest(record.RequestID, this);
			else {
				// If the role is not supported inform the client
				// and don't begin the request.
				Logger.Write (LogLevel.Warning, Strings.Connection_RoleNotSupported, body.Role);
				EndRequest (record.RequestID, 0, ProtocolStatus.UnknownRole);
				return;
			}

			lock (request_lock) {
				requests.Add (request);
			}

			keep_alive = (body.Flags & BeginRequestFlags.KeepAlive) != 0;
		}

		public void SendRecord (RecordType type, ushort requestID,
		                        byte [] bodyData)
		{
			SendRecord (type, requestID, bodyData, 0, bodyData.Length);
		}
		
		public void SendRecord (RecordType type, ushort requestID,
		                        byte [] bodyData, int bodyIndex,
		                        int bodyLength)
		{
			if (IsConnected)
				lock (send_lock) {
					try {
						CompatArraySegment<byte> body = send_buffers.EnforceBodyLength(bodyLength);
						Array.Copy(bodyData, bodyIndex, body.Array, body.Offset, bodyLength);
						var record = new NRecord (1, type, requestID, bodyLength, send_buffers);
						record.Send (socket);
					} catch (System.Net.Sockets.SocketException) {
					}
				}
		}
		
		public void EndRequest (ushort requestID, int appStatus,
		                        ProtocolStatus protocolStatus)
		{
			var body = new EndRequestBody (appStatus, protocolStatus);
			try {	
				if (IsConnected) {
					byte[] bodyData = body.GetData ();
					CompatArraySegment<byte> bodyBuffer = send_buffers.EnforceBodyLength(bodyData.Length);
					Array.Copy(bodyData, 0, bodyBuffer.Array, bodyBuffer.Offset, bodyData.Length);
					var record = new NRecord (1, RecordType.EndRequest, requestID, bodyData.Length, send_buffers);
					record.Send (socket);
				}
			} catch (System.Net.Sockets.SocketException) {
			}
				
			RemoveRequest(requestID);	

			lock (connection_teardown_lock) {
				if (requests.Count == 0 && (!keep_alive || stop)) {
					CloseSocket ();

					if (!stop)
						server.EndConnection (this);

					receive_buffers.Return ();
					send_buffers.Return ();
				}
			}
		}
		
		public void Stop ()
		{
			stop = true;
			lock(request_lock)
				foreach (Request req in new List<Request>(requests))
					EndRequest (req.RequestID, -1, ProtocolStatus.RequestComplete);
		}
		
		#endregion
		
		
		
		#region Private Properties
		
		bool UnfinishedRequests {
			get {
				lock(request_lock)
					foreach (Request request in requests)
						if (request.DataNeeded)
							return true;
				
				return false;
			}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		Request GetRequest (ushort requestID)
		{
			lock(request_lock)
				foreach (Request request in requests)
					if (request.RequestID == requestID)
						return request;
			
			return null;
		}
		
		void RemoveRequest (ushort requestID)
		{
			int i = 0;
			lock(request_lock) {
				int count = requests.Count;
				while (i < count && requests [i].RequestID != requestID)
					i ++;
				if (i != count)
					requests.RemoveAt(i);
			}
		}
		
		void StopRun (string message, params object [] args)
		{
			Logger.Write (LogLevel.Error, message, args);
			Logger.Write (LogLevel.Error,
				Strings.Connection_Terminating);
			
			Stop ();
		}
		
		#endregion
	}
}
