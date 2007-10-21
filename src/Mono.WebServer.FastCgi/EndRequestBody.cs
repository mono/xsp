//
// Record.cs: Represents the FastCGI EndRequestBody structure.
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
	///    Specifies the protocol status at the end of a request.
	/// </summary>
	public enum ProtocolStatus : byte
	{
		/// <summary>
		///    The request was completed successfully.
		/// </summary>
		RequestComplete = 0,
		
		/// <summary>
		///    The request cannot be complete because it would require
		///    multiplexing.
		/// </summary>
		CantMultiplexConnections = 1,
		
		/// <summary>
		///    The request cannot be completed becuase a resource is
		///    overloaded.
		/// </summary>
		Overloaded = 2,
		
		/// <summary>
		///    The request cannot be completed becuase the role is
		///    unknown.
		/// </summary>
		UnknownRole = 3
	}
	
	/// <summary>
	///    This struct contains the body data for an EndRequest record.
	/// </summary>
	public struct EndRequestBody
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the application status.
		/// </summary>
		int app_status;
		
		/// <summary>
		///    Contains the protocol status.
		/// </summary>
		ProtocolStatus protocol_status;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EndRequestBody" /> with a specified application
		///    status and protocol status.
		/// </summary>
		/// <param name="appStatus">
		///    <para>A <see cref="int" /> containing the application
		///    status the request ended with.</para>
		///    <para>This is the same value as would be returned by a
		///    program on termination. On successful termination, this
		///    would be zero.</para>
		/// </param>
		/// <param name="protocolStatus">
		///    A <see cref="ProtocolStatus" /> containing the FastCGI
		///    protocol status with which the request is being ended.
		/// </param>
		public EndRequestBody (int appStatus,
		                       ProtocolStatus protocolStatus)
		{
			app_status      = appStatus;
			protocol_status = protocolStatus;
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
			uint app;
			unchecked {
				app = (uint) app_status;
			}
			byte [] data = new byte [8];
			data [0] = (byte)((app >> 24) & 0xFF);
			data [1] = (byte)((app >> 16) & 0xFF);
			data [2] = (byte)((app >>  8) & 0xFF);
			data [3] = (byte)((app      ) & 0xFF);
			data [4] = (byte) protocol_status;
			return data;
		}
		
		#endregion
	}
}
