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
using System.Globalization;

namespace Mono.FastCgi {
	public enum RecordType : byte {
		None            =  0,
		
		BeginRequest    =  1,
		
		AbortRequest    =  2,
		
		EndRequest      =  3,
		
		Params          =  4,
		
		StandardInput   =  5,
		
		StandardOutput  =  6,
		
		StandardError   =  7,
		
		Data            =  8,
		
		GetValues       =  9,
		
		GetValuesResult = 10,
		
		UnknownType     = 11
	}
	
	public struct Record
	{
		#region Private Fields
		
		private byte version;
		
		private RecordType type;
		
		private ushort request_id;
		
		private byte [] data;
		
		private int body_index;
		
		private ushort body_length;
		
		public const int SuggestedBufferSize = 0x08 + 0xFFFF + 0xFF;
		
		#endregion
		
		
		
		#region Public Fields
		
		public const int HeaderSize = 8;
		
		#endregion
		
		
		
		#region Constructors
		
		public Record (Socket socket) : this (socket, null)
		{
		}
		
		public Record (Socket socket, byte [] buffer)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");
			
			byte[] header_buffer = (buffer != null && buffer.Length
				> 8) ? buffer : new byte [HeaderSize];
			byte   padding_length;
			
			// Read the 8 byte record header.
			ReceiveAll (socket, header_buffer, HeaderSize);
			
			// Read the values from the data.
			version        = header_buffer [0];
			type           = (RecordType) header_buffer [1];
			request_id     = ReadUInt16 (header_buffer, 2);
			body_length    = ReadUInt16 (header_buffer, 4);
			padding_length = header_buffer [6];
			
			int total_length = body_length + padding_length;
			
			data  = (buffer != null && buffer.Length >= total_length)
				? buffer : new byte [total_length];
			body_index = 0;
			
			// Read the record data, and throw an exception if the
			// complete data cannot be read.
			if (total_length > 0)
				ReceiveAll (socket, data, total_length);
			
			Logger.Write (LogLevel.Debug,
				Strings.Record_Received,
				Type, RequestID, BodyLength);
		}
		
		public Record (byte version, RecordType type, ushort requestID,
		               byte [] bodyData) : this (version, type,
		                                         requestID, bodyData,
		                                         0, -1)
		{
		}
		
		public Record (byte version, RecordType type, ushort requestID,
		               byte [] bodyData, int bodyIndex, int bodyLength)
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
					"data");
			
			
			this.version     = version;
			this.type        = type;
			this.request_id  = requestID;
			this.data        = bodyData;
			this.body_index  = bodyIndex;
			this.body_length = (ushort) bodyLength;
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
		
		public ushort BodyLength {
			get {return body_length;}
		}
		#endregion
		
		
		
		#region Public Methods
		
		public void CopyTo (byte[] dest, int destIndex)
		{
			if (dest == null)
				throw new ArgumentNullException ("dest");
			
			if (body_length > dest.Length - destIndex)
				throw new ArgumentOutOfRangeException (
					"destIndex");
			
			Array.Copy (data, body_index, dest, destIndex,
				body_length);
		}
		
		public byte[] GetBody ()
		{
			byte[] body_data = new byte [body_length];
			Array.Copy (data, body_index, body_data, 0,
				body_length);
			return body_data;
		}
		
		public override string ToString ()
		{
			return string.Format (CultureInfo.CurrentCulture,
				Strings.Record_ToString,
				Version, Type, RequestID, BodyLength);
		}
		
		public void Send (Socket socket)
		{
			Send (socket, null);
		}
		
		public void Send (Socket socket, byte [] buffer)
		{
			byte padding_size = (byte) ((8 - (body_length % 8)) % 8);
			
			int total_size = 8 + body_length + padding_size;
			
			byte[] data = (buffer != null && buffer.Length >
				total_size) ? buffer : new byte [total_size];
			
			data [0] = version;
			data [1] = (byte) type;
			data [2] = (byte) (request_id >> 8);
			data [3] = (byte) (request_id & 0xFF);
			data [4] = (byte) (body_length >> 8);
			data [5] = (byte) (body_length & 0xFF);
			data [6] = padding_size;
			
			Array.Copy (this.data, body_index, data, 8,
				body_length);
			
			for (int i = 0; i < padding_size; i ++)
				data [8 + body_length + i] = 0;
			
			Logger.Write (LogLevel.Debug,
				Strings.Record_Sent,
				Type, RequestID, body_length);
			
			SendAll (socket, data, total_size);
		}
		
		#endregion
		
		
		
		#region Internal Static Methods
		
		internal static ushort ReadUInt16 (byte [] array,
		                                   int arrayIndex)
		{
			ushort value = array [arrayIndex];
			value = (ushort) (value << 8);
			value += array [arrayIndex + 1];
			return value;
		}
		
		#endregion
		
		
		
		#region Private Static Methods
		
		private static void ReceiveAll (Socket socket, byte [] data, int length)
		{
			if (length <= 0)
				return;
			
			int total = 0;
			while (total < length) {
				total += socket.Receive (data, total,
					length - total,
					System.Net.Sockets.SocketFlags.None);
			}
		}
		
		private static void SendAll (Socket socket, byte [] data, int length)
		{
			if (length <= 0)
				return;
			
			int total = 0;
			while (total < length) {
				total += socket.Send (data, total,
					length - total,
					System.Net.Sockets.SocketFlags.None);
			}
		}
		
		#endregion
	}
}
