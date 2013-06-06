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

namespace Mono.FastCgi {
	public enum ProtocolStatus : byte
	{
		RequestComplete = 0,
		
		CantMultiplexConnections = 1,
		
		Overloaded = 2,
		
		UnknownRole = 3
	}
	
	public struct EndRequestBody
	{
		#region Private Fields

		readonly int app_status;

		readonly ProtocolStatus protocol_status;
		
		#endregion
		
		
		
		#region Constructors
		
		public EndRequestBody (int appStatus,
		                       ProtocolStatus protocolStatus)
		{
			app_status      = appStatus;
			protocol_status = protocolStatus;
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		public byte [] GetData ()
		{
			uint app;
			unchecked {
				app = (uint) app_status;
			}
			var data = new byte [8];
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
