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
	/// <summary>
	///    Specifies the type of information contained in the record.
	/// </summary>
	public enum RecordType : byte {
		/// <summary>
		///    No record type specified.
		/// </summary>
		None            =  0,
		
		/// <summary>
		///    The record contains the beginning of a request. Sent by
		///    the client.)
		/// </summary>
		BeginRequest    =  1,
		
		/// <summary>
		///    The record is informing that a request has been aborted.
		///    (Sent by the client.)
		/// </summary>
		AbortRequest    =  2,
		
		/// <summary>
		///    The record contains the end of a request. (Sent by the
		///    server.)
		/// </summary>
		EndRequest      =  3,
		
		/// <summary>
		///    The record contains the parameters for a request. (Sent 
		///    by the client.)
		/// </summary>
		Params          =  4,
		
		/// <summary>
		///    The record contains standard input for a request. (Sent 
		///    by the client.)
		/// </summary>
		StandardInput   =  5,
		
		/// <summary>
		///    The record contains standard output for a request. (Sent 
		///    by the server.)
		/// </summary>
		StandardOutput  =  6,
		
		/// <summary>
		///    The record contains standard error for a request. (Sent 
		///    by the server.)
		/// </summary>
		StandardError   =  7,
		
		/// <summary>
		///    The record contains file contents for a request. (Sent 
		///    by the client.)
		/// </summary>
		Data            =  8,
		
		/// <summary>
		///    The record contains a request for server values. (Sent 
		///    by the client.)
		/// </summary>
		GetValues       =  9,
		
		/// <summary>
		///    The record contains a server values. (Sent by the
		///    server.)
		/// </summary>
		GetValuesResult = 10,
		
		/// <summary>
		///    The record contains a notice of failure to recognize a
		///    record type. (Sent by the server.)
		/// </summary>
		UnknownType     = 11
	}
	
	/// <summary>
	///    This struct sends and receives FastCGI records.
	/// </summary>
	public struct Record
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the FastCGI version.
		/// </summary>
		private byte version;
		
		/// <summary>
		///    Contains the record type.
		/// </summary>
		private RecordType type;
		
		/// <summary>
		///    Contains the request ID.
		/// </summary>
		private ushort request_id;
		
		/// <summary>
		///    Contains the record data plus padding.
		/// </summary>
		/// <remarks>
		///    The actual body is stored starting at <see
		///    cref="body_index" /> with a length of <see
		///    cref="body_length" /> bytes of this property.
		/// </remarks>
		private byte [] data;
		
		/// <summary>
		///    Contains the index at which the body begins.
		/// </summary>
		private int body_index;
		
		/// <summary>
		///    Contains the length of the body.
		/// </summary>
		private ushort body_length;
		
		/// <summary>
		///    Contains the suggested buffer size, equal to the maximum
		///    possible size of a record.
		/// </summary>
		public const int SuggestedBufferSize = 0x08 + 0xFFFF + 0xFF;
		
		#endregion
		
		
		
		#region Public Fields
		
		/// <summary>
		///    The size of a FastCGI record header.
		/// </summary>
		public const int HeaderSize = 8;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Record" /> by reading the contents from a specified
		///    socket.
		/// </summary>
		/// <param name="socket">
		///    A <see cref="Socket" /> object to receive the record data
		///    from.
		/// </param>
		/// <remarks>
		///    To improve performance, consider using a buffer and
		///    <see cref="Record(Socket,byte[])" /> instead.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="socket" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="socket" /> does not contain a complete
		///    record.
		/// </exception>
		public Record (Socket socket) : this (socket, null)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Record" /> by reading the contents from a specified
		///    socket.
		/// </summary>
		/// <param name="socket">
		///    A <see cref="Socket" /> object to receive the record data
		///    from.
		/// </param>
		/// <param name="buffer">
		///    A <see cref="byte[]" /> containing the buffer to use when
		///    receiving from the socket or <see langword="null" /> to
		///    create the buffers on the fly.
		/// </param>
		/// <remarks>
		///    <para>If <paramref name="buffer" /> is not <see
		///    langword="null" />, the suggested size is <see
		///    cref="SuggestedBufferSize" />. If the size of the buffer
		///    is insufficient to read the data, a sufficiently sized
		///    array will be created on a per-instance basis.</para>
		///    <note type="caution">
		///       <para>If a buffer is used, the new instance
		///       is only valid until the same buffer is used again.
		///       Therefore, use an extra degree of caution when using
		///       this constructor.</para>
		///    </note>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="socket" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="socket" /> does not contain a complete
		///    record.
		/// </exception>
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
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Record" /> populating it with a specified version,
		///    type, ID, and body.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> containing the FastCGI version the
		///    record is structured for.
		/// </param>
		/// <param name="type">
		///    A <see cref="RecordType" /> containing the type of
		///    record to create.
		/// </param>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    associated with the new record.
		/// </param>
		/// <param name="bodyData">
		///    A <see cref="byte[]" /> containing the contents to use
		///    in the new record.
		/// </param>
		/// <remarks>
		///    <note type="caution">
		///       The new instance will store a reference to <paramref
		///       name="bodyData" /> and as such be invalid when the
		///       value changes externally.
		///    </note>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="bodyData" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="bodyData" /> contains more than 65535
		///    bytes and cannot be sent.
		/// </exception>
		public Record (byte version, RecordType type, ushort requestID,
		               byte [] bodyData) : this (version, type,
		                                         requestID, bodyData,
		                                         0, -1)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Record" /> populating it with a specified version,
		///    type, ID, and body.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> containing the FastCGI version the
		///    record is structured for.
		/// </param>
		/// <param name="type">
		///    A <see cref="RecordType" /> containing the type of
		///    record to create.
		/// </param>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the ID of the request
		///    associated with the new record.
		/// </param>
		/// <param name="bodyData">
		///    A <see cref="byte[]" /> containing the contents to use
		///    in the new record.
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
		///    <note type="caution">
		///       The new instance will store a reference to <paramref
		///       name="bodyData" /> and as such be invalid when the
		///       value changes externally.
		///    </note>
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
		
		/// <summary>
		///    Gets the FastCGI version of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> containing the FastCGI version of
		///    the current instance.
		/// </value>
		public byte Version {
			get {return version;}
		}
		
		/// <summary>
		///    Gets the FastCGI record type of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> containing the FastCGI record type
		///    of the current instance.
		/// </value>
		public RecordType Type {
			get {return type;}
		}
		
		/// <summary>
		///    Gets the ID of the request associated with the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> containing the ID of the request
		///    associated with the current instance.
		/// </value>
		public ushort RequestID {
			get {return request_id;}
		}
		
		/// <summary>
		///    Gets the length of the body of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> containing the body length of the
		///    current instance.
		/// </value>
		public ushort BodyLength {
			get {return body_length;}
		}
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Copies the body to another array.
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
			
			if (body_length > dest.Length - destIndex)
				throw new ArgumentOutOfRangeException (
					"destIndex");
			
			Array.Copy (data, body_index, dest, destIndex,
				body_length);
		}
		
		/// <summary>
		///    Gets the body data of with the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="byte[]" /> containing the body data of
		///    the current instance.
		/// </returns>
		public byte[] GetBody ()
		{
			byte[] body_data = new byte [body_length];
			Array.Copy (data, body_index, body_data, 0,
				body_length);
			return body_data;
		}
		
		/// <summary>
		///    Creates and returns a <see cref="string" />
		///    representation of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> representation of the current
		///    instance.
		/// </value>
		public override string ToString ()
		{
			return string.Format (CultureInfo.CurrentCulture,
				Strings.Record_ToString,
				Version, Type, RequestID, BodyLength);
		}
		
		/// <summary>
		///    Sends a FastCGI record with the data from the current
		///    instance over a given socket.
		/// </summary>
		/// <param name="socket">
		///    A <see cref="Socket" /> object to send the data over.
		/// </param>
		public void Send (Socket socket)
		{
			Send (socket, null);
		}
		
		/// <summary>
		///    Sends a FastCGI record with the data from the current
		///    instance over a given socket.
		/// </summary>
		/// <param name="socket">
		///    A <see cref="Socket" /> object to send the data over.
		/// </param>
		/// <param name="buffer">
		///    A <see cref="byte[]" /> to write the record to or <see
		///    langword="null" /> to create a temporary buffer during
		///    the send.
		/// </param>
		/// <remarks>
		///    If <paramref name="buffer" /> is of insufficient size to
		///    write to the buffer, a temporary buffer will be created.
		/// </remarks>
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
		
		/// <summary>
		///    Reads two bytes of data from an array and returns the
		///    appropriate value.
		/// </summary>
		/// <param name="array">
		///    A <see cref="byte[]" /> containing an array of data to
		///    read from.
		/// </param>
		/// <param name="arrayIndex">
		///    A <see cref="int" /> specifying the index in the array at
		///    which to start reading.
		/// </param>
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
