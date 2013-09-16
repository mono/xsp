//
// Record.cs
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (C) 2007 Brian Nickel
// Copyright (c) 2013 Leonardo Taglialegne.
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
using Mono.FastCgi;
using Mono.WebServer.FastCgi.Compatibility;

namespace Mono.WebServer.FastCgi {
	struct Record
	{
		public readonly byte Version;
		public readonly RecordType Type;
		public readonly ushort RequestID;
		public readonly ushort BodyLength;
		public readonly Buffers Content;

		public const int HEADER_SIZE = 8;

		public Record (byte version, RecordType type, ushort requestID, int bodyLength, Buffers buffers) : this()
		{
			Version = version;
			Type = type;
			RequestID  = requestID;
			BodyLength = (ushort) bodyLength;
			Content = buffers;
		}

		public static Record Receive (Socket socket, Buffers buffers)
		{
			if (socket == null)
				throw new ArgumentNullException ("socket");

			CompatArraySegment<byte> header_buffer = buffers.EnforceHeaderLength (HEADER_SIZE);

			// Read the 8 byte record header.
			ReceiveAll (socket, header_buffer, HEADER_SIZE);

			// Read the values from the data.
			var version         = header_buffer [0];
			var type            = (RecordType) header_buffer [1];
			var requestID       = ReadUInt16 (header_buffer, 2);
			var bodyLength      = ReadUInt16 (header_buffer, 4);
			byte padding_length = header_buffer [6];

			CompatArraySegment<byte> body_buffer  = buffers.EnforceBodyLength (bodyLength);

			// Read the record data, and throw an exception if the
			// complete data cannot be read.
			if (bodyLength > 0)
				ReceiveAll (socket, body_buffer, bodyLength);

			CompatArraySegment<byte> padding_buffer = buffers.EnforcePaddingLength (padding_length);

			if(padding_length > 0)
				ReceiveAll(socket, padding_buffer, padding_length);

			Logger.Write (LogLevel.Debug, Strings.Record_Received, type, requestID, bodyLength);

			return new Record (version, type, requestID, bodyLength, buffers);
		}

		public void Send (Socket socket)
		{
			var padding_size = (byte) ((8 - (BodyLength % 8)) % 8);

			CompatArraySegment<byte> header = Content.EnforceHeaderLength (HEADER_SIZE);

			header [0] = Version;
			header [1] = (byte) Type;
			header [2] = (byte) (RequestID >> 8);
			header [3] = (byte) (RequestID & 0xFF);
			header [4] = (byte) (BodyLength >> 8);
			header [5] = (byte) (BodyLength & 0xFF);
			header [6] = padding_size;

			CompatArraySegment<byte> padding = Content.EnforcePaddingLength (padding_size);

			for (int i = 0; i < padding_size; i ++)
				padding [i] = 0;

			Logger.Write (LogLevel.Debug, Strings.Record_Sent, Type, RequestID, BodyLength);

			SendAll (socket, header, HEADER_SIZE);
			SendAll (socket, Content.Body, BodyLength);
			SendAll (socket, padding, padding_size);
		}

		[Obsolete]
		public static explicit operator Record (Mono.FastCgi.Record source)
		{
			return new Record (source.Version, source.Type, source.RequestID, source.BodyLength, source.GetBuffers ());
		}

		public IReadOnlyList<byte> GetBody ()
		{
			if (Content.Body == null)
				return null;
			return Content.Body.Value.Trimmed (BodyLength);
		}

		[Obsolete]
		public void CopyTo (byte[] dest, int destIndex)
		{
			if (dest == null)
				throw new ArgumentNullException ("dest");

			if (BodyLength > dest.Length - destIndex)
				throw new ArgumentOutOfRangeException ("destIndex");

			if (Content.Body.HasValue)
				Content.Body.Value.CopyTo (dest, destIndex, BodyLength);
		}

		public override string ToString ()
		{
			return String.Format (CultureInfo.CurrentCulture,
			                      Strings.Record_ToString,
			                      Version, Type, RequestID, BodyLength);
		}

		static internal Role ReadRole(IReadOnlyList<byte> array){
			return (Role)ReadUInt16 (array, 0);
		}

		static internal ushort ReadUInt16 (IReadOnlyList<byte> array,
		                          int arrayIndex)
		{
			ushort value = array [arrayIndex];
			value = (ushort) (value << 8);
			value += array [arrayIndex + 1];
			return value;
		}

		internal static void SendAll (Socket socket, CompatArraySegment<byte>? data, int length)
		{
			if (length <= 0 || data == null)
				return;

			int total = 0;
			while (total < length) {
				total += socket.Send (data.Value.Array, data.Value.Offset + total,
				                      length - total, System.Net.Sockets.SocketFlags.None);
			}
		}

		internal static void ReceiveAll (Socket socket, CompatArraySegment<byte> data, int length)
		{
			if (length <= 0)
				return;

			int total = 0;
			while (total < length) {
				total += socket.Receive (data.Array, total + data.Offset,
				                         length - total,
				                         System.Net.Sockets.SocketFlags.None);
			}
		}
	}
}

