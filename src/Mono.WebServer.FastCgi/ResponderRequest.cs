//
// Requests/ResponderRequest.cs: Handles FastCGI requests for a responder.
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
using System.Threading;
using Mono.WebServer.FastCgi;

namespace Mono.FastCgi {
	public class ResponderRequest : Request
	{
		#region Private Fields
		
		byte [] input_data;
		
		int write_index;
		
		readonly IResponder responder;
		
		#endregion
		
		
		
		#region Constructors
		
		public ResponderRequest (ushort requestID,
		                         Connection connection)
			: base (requestID, connection)
		{
			if (!Server.SupportsResponder)
				throw new Exception ();
			
			responder = Server.CreateResponder (this);
			
			InputDataReceived  += OnInputDataReceived;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		public byte [] InputData {
			get {return input_data ?? new byte [0];}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		void OnInputDataReceived (Request sender, DataReceivedArgs args)
		{
			// If the data is completed, call the worker and return.
			if (args.DataCompleted) {
				DataNeeded = false;
				
				if (input_data != null &&
					write_index < input_data.Length) {
					Abort (Strings.ResponderRequest_IncompleteInput,
						write_index, input_data.Length);
				}
				else if (Server.MultiplexConnections)
					ThreadPool.QueueUserWorkItem (Worker);
				else
					Worker (null);
				
				return;
			}
			
			// If input_data is null, create the new array by
			// reading the length from the CONTENT_LENGTH parameter.
			if (input_data == null) {
				string length_text = GetParameter ("CONTENT_LENGTH");
				
				// If the field is missing we can't continue.
				if (length_text == null) {
					Abort (Strings.ResponderRequest_NoContentLength);
					return;
				}
				
				// If the length isn't a number, we can't
				// continue.
				int length;
				if(!Int32.TryParse (length_text, NumberStyles.Integer,
					CultureInfo.InvariantCulture, out length)){
					Abort (Strings.ResponderRequest_NoContentLengthNotNumber);
					return;
				}
				
				input_data = new byte [length];
			}
			
			if (write_index + args.DataLength > input_data.Length)
			{
				Abort (Strings.ResponderRequest_ContentExceedsLength);
				return;
			}
			
			args.CopyTo (input_data, write_index);
			write_index += args.DataLength;
		}
		
		void Worker (object state)
		{
			int appStatus = responder.Process ();
			if (appStatus != Int32.MinValue)
				CompleteRequest (appStatus,
					ProtocolStatus.RequestComplete);
		}
		
		#endregion
	}
	
	public interface IResponder
	{
		ResponderRequest Request {get;}
		
		int Process ();
	}
}