//
// Record.cs: Handles sending and receiving FastCGI records via sockets.
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
using System.Globalization;
using Mono.WebServer.Log;
using Mono.WebServer.FastCgi;
using Mono.WebServer.FastCgi.Compatibility;
using NRecord = Mono.WebServer.FastCgi.Record;

namespace Mono.FastCgi {
	[Obsolete]
	public struct Record
	{
		#region Private Fields
		
		readonly byte version;
		
		readonly RecordType type;
		
		readonly ushort request_id;
		
		readonly Buffers buffers;
		
		public const int SuggestedBufferSize = 0x08 + 0xFFFF + 0xFF;
		
		#endregion
		
		
		
		#region Public Fields
		
		public const int HeaderSize = 8;
		
		#endregion
		
		
		
		#region Constructors

		public Record (Socket socket) : this (socket, new Buffers())
		{
		}

		public Record (Socket socket, byte[] buffer) : this (socket, new Buffers (buffer, HeaderSize, buffer.Length - HeaderSize - 8))
		{
		}

		public Record (Socket socket, Buffers receive_buffer) : this()
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");

			CompatArraySegment<byte> header_buffer = receive_buffer.EnforceHeaderLength (HeaderSize);

			// Read the 8 byte record header.
			NRecord.ReceiveAll (socket, header_buffer, HeaderSize);

			// Read the values from the data.
			version        = header_buffer [0];
			type           = (RecordType) header_buffer [1];
			request_id     = NRecord.ReadUInt16 (header_buffer, 2);
			BodyLength     = NRecord.ReadUInt16 (header_buffer, 4);
			byte padding_length = header_buffer [6];

			CompatArraySegment<byte> body_buffer  = receive_buffer.EnforceBodyLength (BodyLength);

			// Read the record data, and throw an exception if the
			// complete data cannot be read.
			if (BodyLength > 0)
				NRecord.ReceiveAll (socket, body_buffer, BodyLength);

			CompatArraySegment<byte> padding_buffer = receive_buffer.EnforcePaddingLength (padding_length);

			if(padding_length > 0)
				NRecord.ReceiveAll(socket, padding_buffer, padding_length);

			buffers = receive_buffer;

			Logger.Write (LogLevel.Debug, Strings.Record_Received, Type, RequestID, BodyLength);
		}

		public Record (byte version, RecordType type, ushort requestID,
		               byte [] bodyData) : this (version, type,
		                                         requestID, bodyData,
		                                         0, -1)
		{
		}

		public Record (byte version, RecordType type, ushort requestID,
		               Buffers buffers, int bodyLength) : this()
		{
			this.version = version;
			this.type = type;
			request_id  = requestID;
			this.buffers = buffers;
			BodyLength = (ushort) bodyLength;
		}
		
		public Record (byte version, RecordType type, ushort requestID,
		               byte [] bodyData, int bodyIndex, int bodyLength) : this()
		{
			if (bodyData == null)
				throw new ArgumentNullException ("bodyData");
			
			if (bodyIndex < 0 || bodyIndex > bodyData.Length)
				throw new ArgumentOutOfRangeException (
					"bodyIndex");
			
			if (bodyLength < 0)
				bodyLength = bodyData.Length - bodyIndex;
			
			if (bodyLength > 0xFFFF)
				throw new ArgumentException (
					Strings.Record_DataTooBig,
					"bodyLength");


			this.version = version;
			this.type = type;
			request_id  = requestID;
			buffers = new Buffers (bodyData, bodyIndex, bodyLength);
			BodyLength = (ushort) bodyLength;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		public byte Version {
			get {return version;}
		}
		
		public RecordType Type {
			get {return type;}
		}
		
		public ushort RequestID {
			get {return request_id;}
		}
		
		public ushort BodyLength { get; private set; }
		#endregion
		
		
		
		#region Public Methods

		internal Buffers GetBuffers ()
		{
			return buffers;
		}
		
		public void CopyTo (byte[] dest, int destIndex)
		{
			if (dest == null)
				throw new ArgumentNullException ("dest");
			
			if (BodyLength > dest.Length - destIndex)
				throw new ArgumentOutOfRangeException ("destIndex");

			if (buffers.Body.HasValue)
				buffers.Body.Value.CopyTo (dest, destIndex, BodyLength);
		}
		
		public byte[] GetBody ()
		{
			var body_data = new byte [BodyLength];
			if(buffers.Body.HasValue)
				buffers.Body.Value.CopyTo (body_data, 0, BodyLength);
			return body_data;
		}

		public void GetBody (out IReadOnlyList<byte> body)
		{
			if (buffers.Body == null)
				body = null;
			else
				body = buffers.Body.Value.Trimmed (BodyLength);
		}

		public override string ToString ()
		{
			return String.Format (CultureInfo.CurrentCulture,
				Strings.Record_ToString,
				Version, Type, RequestID, BodyLength);
		}
		
		public void Send (Socket socket, byte [] buffer)
		{
			Send(socket);
		}

		public void Send(Socket socket)
		{
			var padding_size = (byte) ((8 - (BodyLength % 8)) % 8);

			CompatArraySegment<byte> header = buffers.EnforceHeaderLength (HeaderSize);

			header [0] = version;
			header [1] = (byte) type;
			header [2] = (byte) (request_id >> 8);
			header [3] = (byte) (request_id & 0xFF);
			header [4] = (byte) (BodyLength >> 8);
			header [5] = (byte) (BodyLength & 0xFF);
			header [6] = padding_size;

			CompatArraySegment<byte> padding = buffers.EnforcePaddingLength (padding_size);

			for (int i = 0; i < padding_size; i ++)
				padding [i] = 0;

			Logger.Write (LogLevel.Debug, Strings.Record_Sent, Type, RequestID, BodyLength);

			NRecord.SendAll (socket, header, HeaderSize);
			NRecord.SendAll (socket, buffers.Body, BodyLength);
			NRecord.SendAll (socket, padding, padding_size);
		}

		#endregion
	}
}
