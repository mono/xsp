//
// Record.cs: Represents the FastCGI BeginRequestBody structure.
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

namespace Mono.FastCgi {
	/// <summary>
	///    This struct contains the body data for an UnknownType record.
	/// </summary>
	/// <remarks>
	///    An UnknownType record is sent by the server when the client sends
	///    it a type that it does not know how to handle.
	/// </remarks>
	public struct UnknownTypeBody
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the unknown type.
		/// </summary>
		private RecordType type;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnknownTypeBody" /> for a specified type.
		/// </summary>
		/// <param name="unknownType">
		///    A <see cref="RecordType" /> containing the unknown type.
		/// </param>
		public UnknownTypeBody (RecordType unknownType)
		{
			type = unknownType;
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Gets the data contained in the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="byte[]" /> containing the data contained in
		///    the current instance.
		/// </returns>
		public byte [] GetData ()
		{
			byte [] data = new byte [8];
			data [0] = (byte) type;
			return data;
		}
		
		#endregion
	}
}
