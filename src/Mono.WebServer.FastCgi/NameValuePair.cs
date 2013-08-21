//
// NameValuePair.cs: Handles the parsing of FastCGI name/value pairs.
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
using System.Text;
using System.Collections.Generic;
using Mono.WebServer.FastCgi;
using Mono.WebServer.Log;
using Mono.WebServer.FastCgi.Compatibility;

namespace Mono.FastCgi {
	public struct NameValuePair
	{
		#region Private Fields
		
		readonly string name;
		
		readonly string value;
		
		static Encoding encoding = Encoding.Default;
		
		#endregion
		
		
		
		#region Public Fields
		
		public static readonly NameValuePair Empty = new NameValuePair (
			null, null);
		
		#endregion
		
		
		
		#region Constructors
		
		public NameValuePair (string name, string value)
		{
			this.name  = name;
			this.value = value;
		}

		[Obsolete]
		public NameValuePair (byte [] data, ref int index)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			// Name/value pairs are stored with their lengths first,
			// then their contents.
			
			// Lengths are stored in 1 or 4 bytes depending on the
			// size of the contents.
			int name_length  = ReadLength (data.ToReadOnlyList (), ref index);
			int value_length = ReadLength (data.ToReadOnlyList (), ref index);
			
			// Do a sanity check on the size of the data.
			if (index + name_length + value_length > data.Length)
				throw new ArgumentOutOfRangeException ("index");
			
			// Make sure the encoding doesn't change while running.
			Encoding enc = encoding;
			
			// Read the name.
			name = enc.GetString (data, index, name_length);
			index += name_length;
			
			// Read the value.
			value = enc.GetString (data, index, value_length);
			index += value_length;
			
			Logger.Write (LogLevel.Debug,
				Strings.NameValuePair_ParameterRead,
				name, value);
		}

		public NameValuePair(IReadOnlyList<byte> data, ref int index)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			// Name/value pairs are stored with their lengths first,
			// then their contents.

			// Lengths are stored in 1 or 4 bytes depending on the
			// size of the contents.
			int name_length = ReadLength(data, ref index);
			int value_length = ReadLength(data, ref index);

			// Do a sanity check on the size of the data.
			if (index + name_length + value_length > data.Count)
				throw new ArgumentOutOfRangeException("index");

			// Make sure the encoding doesn't change while running.
			Encoding enc = encoding;

			// Read the name.
			// FIXME: Please, PLEASE fix me
			var segment = (CompatArraySegment<byte>)data;
			name = enc.GetString(segment.Array, segment.Offset + index, name_length);
			index += name_length;

			// Read the value.
			value = enc.GetString(segment.Array, segment.Offset +index, value_length);
			index += value_length;

			Logger.Write(LogLevel.Debug,
				Strings.NameValuePair_ParameterRead,
				name, value);
		}

		#endregion
		
		
		
		#region Public Properties
		
		public string Name {
			get {return name;}
		}
		
		public string Value {
			get {return value;}
		}
		
		#endregion
		
		
		
		#region Public Static Properties
		
		public static Encoding Encoding {
			get {return encoding;}
			set {encoding = value ?? Encoding.Default;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods

		[Obsolete]
		public static IDictionary<string,string> FromData (byte [] data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			// Specialized.NameValueCollection would probably be
			// better, but it doesn't implement IDictionary.
			var pairs = new Dictionary<string,string> ();
			int index = 0;
			
			// Loop through the array, reading pairs at a specified
			// position until the end is reached.
			
			while (index < data.Length) {
				var pair = new NameValuePair (data, ref index);
				
				if (pairs.ContainsKey (pair.Name)) {
					Logger.Write (LogLevel.Warning,
						Strings.NameValuePair_DuplicateParameter,
						pair.Name);
					
					pairs [pair.Name] = pair.Value;
				} else
					pairs.Add (pair.Name, pair.Value);
			}
			
			return pairs;
		}

		public static IDictionary<string, string> FromData(IReadOnlyList<byte> data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			// Specialized.NameValueCollection would probably be
			// better, but it doesn't implement IDictionary.
			var pairs = new Dictionary<string, string>();
			int index = 0;

			// Loop through the array, reading pairs at a specified
			// position until the end is reached.

			while (index < data.Count)
			{
				var pair = new NameValuePair(data, ref index);

				if (pairs.ContainsKey(pair.Name))
				{
					Logger.Write(LogLevel.Warning,
						Strings.NameValuePair_DuplicateParameter,
						pair.Name);

					pairs[pair.Name] = pair.Value;
				}
				else
					pairs.Add(pair.Name, pair.Value);
			}

			return pairs;
		}
		public static byte [] GetData (IDictionary<string,string> pairs)
		{
			if (pairs == null)
				throw new ArgumentNullException ("pairs");
			
			// Make sure the encoding doesn't change while running.
			Encoding enc = encoding;
			
			// Get the total size of the new array and validate the
			// contents of "pairs".
			
			int total_size = 0;
			
			foreach (string key in pairs.Keys)
			{
				string value = pairs [key];
				
				// Sanity check: "pairs" must only contain
				// strings.
				if (key == null || value == null)
					throw new ArgumentException (
						Strings.NameValuePair_DictionaryContainsNonString,
						"pairs");
				
				int name_length = enc.GetByteCount (key);
				int value_length = enc.GetByteCount (value);
				
				total_size += name_length > 0x7F ? 4 : 1;
				total_size += value_length > 0x7F ? 4 : 1;
				total_size += name_length + value_length;
			}
			
			var data = new byte [total_size];
			
			// Fill the data array with the data.
			int index = 0;
			
			foreach (string key in pairs.Keys)
			{
				string value = pairs [key];
				
				int name_length = enc.GetByteCount (key);
				int value_length = enc.GetByteCount (value);
				
				WriteLength (data, ref index, name_length);
				WriteLength (data, ref index, value_length);
				index += enc.GetBytes (key, 0, key.Length, data, index);
				index += enc.GetBytes (value, 0, value.Length, data, index);
			}
			
			return data;
		}
		
		#endregion
		
		
		
		#region Private Static Methods
		
		static int ReadLength (IReadOnlyList<byte> data, ref int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");
			
			if (index >= data.Count)
				throw new ArgumentOutOfRangeException ("index");
			
			// Lengths are stored in either 1 or 4 bytes. For
			// lengths under 128 bytes, which are the most common, a
			// single byte is used.
			
			if (data [index] < 0x80)
				return data [index++];
			
			// If the MSB for the size byte is set (value >= 0x80),
			// a 4 byte value is used. However, the MSB in the first
			// byte is not included, as it was used as an indicator.
			
			if (index > data.Count - 4)
				throw new ArgumentOutOfRangeException ("index");
			
			
			return (0x7F & data [index++]) * 0x1000000
				+ data [index++] * 0x10000
				+ data [index++] *0x100
				+ data [index++];
			
			// TODO: Returns zero. What gives?
			//return (0x7F & (int) data [index++]) << 24
			//	+ ((int) data [index++]) << 16
			//	+ ((int) data [index++]) <<  8
			//	+ ((int) data [index++]);
		}
		
		static void WriteLength (byte [] data, ref int index,
		                                 int length)
		{
			if (length < 0)
				throw new ArgumentException (
					Strings.NameValuePair_LengthLessThanZero,
					"length");
			
			if (index < 0 ||
				index > data.Length - (length < 0x80 ? 1 : 4))
				throw new ArgumentOutOfRangeException ("index");
			
			if (length < 0x80) {
				data [index++] = (byte) length;
				return;
			}
			
			data [index++] = (byte)(((length & 0x7F000000) >> 24) | 0x80);
			data [index++] = (byte) ((length & 0xFF0000) >> 16);
			data [index++] = (byte) ((length & 0xFF00) >> 8);
			data [index++] = (byte)  (length & 0xFF);
		}
		
		#endregion
	}
}