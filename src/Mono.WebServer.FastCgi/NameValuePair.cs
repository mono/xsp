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
#if NET_2_0
using System.Collections.Generic;
#else
using System.Collections;
#endif

namespace Mono.FastCgi {
	/// <summary>
	///    This struct reads and writes FastCGI name/value pairs.
	/// </summary>
	public struct NameValuePair
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the name of the pair.
		/// </summary>
		private string name;
		
		/// <summary>
		///    Contains the value of the pair.
		/// </summary>
		private string value;
		
		/// <summary>
		///    Contains the encoding to use when reading and writing
		///    params.
		/// </summary>
		private static Encoding encoding = Encoding.Default;
		
		#endregion
		
		
		
		#region Public Fields
		
		/// <summary>
		///    A contstant representation of an empty <see
		///    cref="NameValuePair" />.
		/// </summary>
		/// <value>
		///    An empty <see cref="NameValuePair" />.
		/// </value>
		public static readonly NameValuePair Empty = new NameValuePair (
			null, null);
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="NameValuePair" /> with a specified name and value.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> containing the name for the
		///    new instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="string" /> containing the value for the
		///    new instance.
		/// </param>
		public NameValuePair (string name, string value)
		{
			this.name  = name;
			this.value = value;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="NameValuePair" /> reading it from specified data
		///    at a specified index, moving the index to the position of
		///    the next pair.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing the name/value pair to
		///    read.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> specifying the index at which to
		///    read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    The data cannot be read at the <paramref name="index" />
		///    because it would read outside of the array.
		/// </exception>
		public NameValuePair (byte [] data, ref int index)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			// Name/value pairs are stored with their lengths first,
			// then their contents.
			
			// Lengths are stored in 1 or 4 bytes depending on the
			// size of the contents.
			int name_length  = ReadLength (data, ref index);
			int value_length = ReadLength (data, ref index);
			
			// Do a sanity check on the size of the data.
			if (index + name_length + value_length > data.Length)
				throw new ArgumentOutOfRangeException ("index");
			
			// Make sure the encoding doesn't change while running.
			Encoding enc = encoding;
			
			// Read the name.
			this.name = enc.GetString (data, index, name_length);
			index += name_length;
			
			// Read the value.
			this.value = enc.GetString (data, index, value_length);
			index += value_length;
			
			Logger.Write (LogLevel.Debug,
				Strings.NameValuePair_ParameterRead,
				this.name, this.value);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the name of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the name of the
		///    current instance.
		/// </value>
		public string Name {
			get {return name;}
		}
		
		/// <summary>
		///    Gets the value of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the value of the
		///    current instance.
		/// </value>
		public string Value {
			get {return value;}
		}
		
		#endregion
		
		
		
		#region Public Static Properties
		
		/// <summary>
		///    Gets and sets the encoding to use when reading and
		///    writing instances of <see cref="NameValuePair" /> to
		///    memory.
		/// </summary>
		/// <value>
		///    A <see cref="Text.Encoding" /> to use reading and writing
		///    to memory.
		/// </value>
		public static Encoding Encoding {
			get {return encoding;}
			set {encoding = value != null ? value : Encoding.Default;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Reads FastCGI name/value pairs from memory and stores
		///    them as a
		#if NET_2_0
		///    <see cref="T:System.Collections.Generic.IDictionary&lt;string,string&gt;" />.
		#else
		///    <see cref="IDictionary" />.
		#endif
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing a collection of
		///    FastCGI name/value pairs.
		/// </param>
		/// <returns>
		#if NET_2_0
		///    A <see cref="T:System.Collections.Generic.IDictionary&lt;string,string&gt;" />
		#else
		///    A <see cref="IDictionary" />
		#endif
		///    object containing the name/value pairs read from
		///     <paramref name="data" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		#if NET_2_0
		public static IDictionary<string,string> FromData (byte [] data)
		#else
		public static IDictionary FromData (byte [] data)
		#endif
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			// Specialized.NameValueCollection would probably be
			// better, but it doesn't implement IDictionary.
			#if NET_2_0
			Dictionary<string,string> pairs =
				new Dictionary<string,string> ();
			#else
			Hashtable pairs = new Hashtable ();
			#endif
			int index = 0;
			
			// Loop through the array, reading pairs at a specified
			// position until the end is reached.
			
			while (index < data.Length) {
				NameValuePair pair = new NameValuePair
					(data, ref index);
				
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
		
		/// <summary>
		///    Reads name/value pairs from a
		#if NET_2_0
		///    <see cref="T:System.Collections.Generic.IDictionary&lt;string,string&gt;" />
		#else
		///    <see cref="IDictionary" />
		#endif
		///    and stores them as FastCGI name/value pairs.
		/// </summary>
		/// <param name="pairs">
		#if NET_2_0
		///    A <see cref="T:System.Collections.Generic.IDictionary&lt;string,string&gt;" />
		#else
		///    A <see cref="IDictionary" />
		#endif
		///    containing string pairs.
		/// </param>
		/// <returns>
		///    A <see cref="byte[]" /> containing FastCGI name/value
		///    pairs.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="pairs" /> is <see langref="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="pairs" /> contains a name or value not of
		///    type <see cref="string" />.
		/// </exception>
		#if NET_2_0
		public static byte [] GetData (IDictionary<string,string> pairs)
		#else
		public static byte [] GetData (IDictionary pairs)
		#endif
		{
			if (pairs == null)
				throw new ArgumentNullException ("pairs");
			
			// Make sure the encoding doesn't change while running.
			Encoding enc = encoding;
			
			// Get the total size of the new array and validate the
			// contents of "pairs".
			
			int total_size = 0;
			
			#if NET_2_0
			foreach (string key in pairs.Keys)
			#else
			foreach (object key in pairs.Keys)
			#endif
			{
				string name = key as string;
				string value = pairs [key] as string;
				
				// Sanity check: "pairs" must only contain
				// strings.
				if (name == null || value == null)
					throw new ArgumentException (
						Strings.NameValuePair_DictionaryContainsNonString,
						"pairs");
				
				int name_length = enc.GetByteCount (name);
				int value_length = enc.GetByteCount (value);
				
				total_size += name_length > 0x7F ? 4 : 1;
				total_size += value_length > 0x7F ? 4 : 1;
				total_size += name_length + value_length;
			}
			
			byte [] data = new byte [total_size];
			
			// Fill the data array with the data.
			int index = 0;
			
			#if NET_2_0
			foreach (string key in pairs.Keys)
			#else
			foreach (object key in pairs.Keys)
			#endif
			{
				string name = key as string;
				string value = pairs [key] as string;
				
				int name_length = enc.GetByteCount (name);
				int value_length = enc.GetByteCount (value);
				
				WriteLength (data, ref index, name_length);
				WriteLength (data, ref index, value_length);
				index += enc.GetBytes (name, 0, name.Length, data, index);
				index += enc.GetBytes (value, 0, value.Length, data, index);
			}
			
			return data;
		}
		
		#endregion
		
		
		
		#region Private Static Methods
		
		/// <summary>
		///    Reads a FastCGI name/value length from specified data at
		///    a specified position, moving the index to the position
		///    after the length data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> containing a FastCGI name/value
		///    length.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> specifying at what index to read.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> containing the length.
		/// </returns>
		/// <remarks>
		///    For values less than 128, lengths are stored as a single
		///    byte. Otherwise, they are stored as four bytes, with a
		///    maximum value of <see cref="int.MaxValue" />.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="index" /> is less than zero or would
		///    require reading past the end of <paramref name="data" />.
		/// </exception>
		private static int ReadLength (byte [] data, ref int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");
			
			if (index >= data.Length)
				throw new ArgumentOutOfRangeException ("index");
			
			// Lengths are stored in either 1 or 4 bytes. For
			// lengths under 128 bytes, which are the most common, a
			// single byte is used.
			
			if (data [index] < 0x80)
				return data [index++];
			
			// If the MSB for the size byte is set (value >= 0x80),
			// a 4 byte value is used. However, the MSB in the first
			// byte is not included, as it was used as an indicator.
			
			if (index > data.Length - 4)
				throw new ArgumentOutOfRangeException ("index");
			
			
			return (0x7F & (int) data [index++]) * 0x1000000
				+ ((int) data [index++]) * 0x10000
				+ ((int) data [index++]) *0x100
				+ ((int) data [index++]);
			
			// TODO: Returns zero. What gives?
			//return (0x7F & (int) data [index++]) << 24
			//	+ ((int) data [index++]) << 16
			//	+ ((int) data [index++]) <<  8
			//	+ ((int) data [index++]);
		}
		
		/// <summary>
		///    Writes a FastCGI name/value length to specified data at
		///    a specified position, moving the index to the position
		///    after the length data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="byte[]" /> to write to.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> specifying at what index to write.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> containing the length.
		/// </param>
		/// <remarks>
		///    For values less than 128, lengths are stored as a single
		///    byte. Otherwise, they are stored as four bytes, with a
		///    maximum value of <see cref="int.MaxValue" />.
		/// </remarks>
		/// <exception cref="ArgumentException">
		///    <paramref name="length" /> represents a negative length.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="index" /> is less than zero or would
		///    require writing past the end of <paramref name="data" />.
		/// </exception>
		private static void WriteLength (byte [] data, ref int index,
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
			
			data [index++] = (byte) ((length & 0x7F000000) >> 24);
			data [index++] = (byte) ((length & 0xFF0000) >> 16);
			data [index++] = (byte) ((length & 0xFF00) >> 8);
			data [index++] = (byte)  (length & 0xFF);
		}
		
		#endregion
	}
}