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
using Mono.WebServer.FastCgi;
using System.Collections.Generic;

namespace Mono.FastCgi {
	public enum Role : ushort
	{
		Responder  = 1,
		
		Authorizer = 2,
		
		Filter     = 3
	}
	
	[Flags]
	public enum BeginRequestFlags : byte
	{
		None      = 0,
		
		KeepAlive = 1
	}
	
	public struct BeginRequestBody
	{
		#region Private Fields
		
		readonly Role role;
		
		readonly BeginRequestFlags flags;
		
		#endregion
		
		
		
		#region Constructors
		
		public BeginRequestBody (Record record)
		{
			if (record.Type != RecordType.BeginRequest)
				throw new ArgumentException (
					Strings.BeginRequestBody_WrongType,
					"record");
			
			if (record.BodyLength != 8)
				throw new ArgumentException (
					Strings.BeginRequestBody_WrongSize, "record");
			
			IReadOnlyList<byte> body;
			record.GetBody (out body);
			role  = (Role) Record.ReadUInt16 (body, 0);
			flags = (BeginRequestFlags) body [2];
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		public Role Role {
			get {return role;}
		}
		
		public BeginRequestFlags Flags {
			get {return flags;}
		}
		
		#endregion
	}
}
