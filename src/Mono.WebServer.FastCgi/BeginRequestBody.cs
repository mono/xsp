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
	///    Specifies the role to use for a request.
	/// </summary>
	public enum Role : ushort
	{
		/// <summary>
		///    The request is for the role of "Responder".
		/// </summary>
		Responder  = 1,
		
		/// <summary>
		///    The request is for the role of "Authorizer".
		/// </summary>
		Authorizer = 2,
		
		/// <summary>
		///    The request is for the role of "Filter".
		/// </summary>
		Filter     = 3
	}
	
	/// <summary>
	///    Specifies flags to use for a request.
	/// </summary>
	[Flags]
	public enum BeginRequestFlags : byte
	{
		/// <summary>
		///    The request has no flags.
		/// </summary>
		None      = 0,
		
		/// <summary>
		///    The connection is to be kept alive after the request is
		///    complete.
		/// </summary>
		KeepAlive = 1
	}
	
	/// <summary>
	///    This struct contains the body data for a BeginRequest record.
	/// </summary>
	public struct BeginRequestBody
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the role of the request.
		/// </summary>
		private Role role;
		
		/// <summary>
		///    Contains the flags for the request.
		/// </summary>
		private BeginRequestFlags flags;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="BeginRequestBody" /> by reading the body of a
		///    specified record.
		/// </summary>
		/// <param name="record">
		///    A <see cref="Record" /> object containing the body to
		///    read.
		/// </param>
		/// <exception cref="ArgumentException">
		///    <paramref name="record" /> is not of type <see
		///    cref="RecordType.BeginRequest" /> or does not contain
		///    exactly 8 bytes of body data.
		/// </exception>
		public BeginRequestBody (Record record)
		{
			if (record.Type != RecordType.BeginRequest)
				throw new ArgumentException (
					Strings.BeginRequestBody_WrongType,
					"record");
			
			if (record.BodyLength != 8)
				throw new ArgumentException (
					Strings.BeginRequestBody_WrongSize, "record");
			
			byte[] body = record.GetBody ();
			role  = (Role) Record.ReadUInt16 (body, 0);
			flags = (BeginRequestFlags) body [2];
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the role of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="FastCgi.Role" /> containing the role of the
		///    current instance.
		/// </value>
		public Role Role {
			get {return role;}
		}
		
		/// <summary>
		///    Gets the flags contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="BeginRequestFlags" /> containing the flags
		///    contained in the current instance.
		/// </value>
		public BeginRequestFlags Flags {
			get {return flags;}
		}
		
		#endregion
	}
}
