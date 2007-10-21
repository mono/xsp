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

namespace Mono.FastCgi {
	/// <summary>
	///    This class extends <see cref="Request" /> adding on processing
	///    features for the role of Responder.
	/// </summary>
	public class ResponderRequest : Request
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the standard input data for the request.
		/// </summary>
		/// <remarks>
		///    When the input is first read, an array of a size equal to
		///    CONTENT_LENGTH is created and assigned. At that point
		///    <see cref="write_index" /> is set to zero and the array
		///    is filled piece by piece as more input data is received.
		/// </remarks>
		private byte [] input_data;
		
		/// <summary>
		///    Contains the index at which to write the next block of
		///    data in <see cref="input_data" />.
		/// </summary>
		private int write_index;
		
		/// <summary>
		///    Contains the <see cref="IResponder" /> that will respond
		///    to the current instance.
		/// </summary>
		private IResponder responder;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ResponderRequest" /> with the specified request ID
		///    and connection.
		/// </summary>
		/// <param name="requestID">
		///    A <see cref="ushort" /> containing the request ID of the
		///    new instance.
		/// </param>
		/// <param name="connection">
		///    A <see cref="Connection" /> object from which data is
		///    received and to which data will be sent.
		/// </param>
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
		
		/// <summary>
		///    Gets the standard input data sent to the current
		///    instance.
		/// </summary>
		/// <remarks>
		///    The size of this input will be equal to the value of
		///    the CONTENT_LENGTH parameter but will contain zeroed
		///    values at the end before the data is completely read and
		///    <see cref="IResponder.Process" /> is called.
		/// </remarks>
		public byte [] InputData {
			get {return input_data != null ? input_data : new byte [0];}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Handles the <see cref="Request.InputDataReceived" />
		///    event by completing the request is the data is
		///    completed, or creating <see cref="input_data" /> if it
		///    does not exist and filling <see cref="input_data" /> with
		///    the received data.
		/// </summary>
		/// <param name="sender">
		///    A <see cref="Request" /> containing the sender of the
		///    event. Always the current instance.
		/// </param>
		/// <param name="args">
		///    A <see cref="DataReceivedArgs" /> containing the
		///    arguments for the event.
		/// </param>
		private void OnInputDataReceived (Request sender,
		                                  DataReceivedArgs args)
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
				string length_text = this.GetParameter
					("CONTENT_LENGTH");
				
				// If the field is missing we can't continue.
				if (length_text == null) {
					Abort (Strings.ResponderRequest_NoContentLength);
					return;
				}
				
				// If the length isn't a number, we can't
				// continue.
				int length;
				try {
					length = int.Parse (length_text,
						CultureInfo.InvariantCulture);
				} catch {
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
		
		/// <summary>
		///    Processes the request with the responder.
		/// </summary>
		/// <param name="state">
		///    A <see cref="object" /> containing the state as passed to
		///    the method. Always <see langword="null" />.
		/// </param>
		/// <remarks>
		///    If multiplexing is enabled, this method will be called
		///    from the thread queue. Otherwise it will be called in the
		///    same thread as the connection.
		/// </remarks>
		private void Worker (object state)
		{
			int appStatus = responder.Process ();
			if (appStatus != int.MinValue)
				CompleteRequest (appStatus,
					ProtocolStatus.RequestComplete);
		}
		
		#endregion
	}
	
	/// <summary>
	///    This interface is used for classes that will serve as responders.
	/// </summary>
	/// <remarks>
	///    <para>In addition to implementing this interface, a potential
	///    responder must contain a constructor accepting a single parameter
	///    of type <see cref="ResponderRequest" />.</para>
	///    <para>To register a responder with a server, use <see
	///    cref="Server.SetResponder" />.</para>
	/// </remarks>
	/// <example>
	///    A very basic responder:
	///    <code lang="C#">
	///    class MyResponder : IResponder
	///    {
	///        ResponderRequest req;
	///        
	///        public MyResponder (ResponderRequest request)
	///        {
	///            req = request;
	///        }
	///        
	///        public int Process ()
	///        {
	///            req.SendOutput ("Content-Type: text/html\r\n\r\n");
	///            req.SendOutput ("&lt;html>\n &lt;head>&lt;title>Test&lt;/title>&lt;/head>\n");
	///            req.SendOutput (" &lt;body>\n  Server name: ");
	///            req.SendOutput (GetParameter ("SERVER_NAME"));
	///            req.SendOutput ("\n &lt;/body>\n&lt;/html>");
	///            return 0;
	///        }
	///        
	///        public ResponderRequest Request {
	///            get {return req;}
	///        }
	///    }
	///    
	///    ...
	///    
	///    server.SetResponder (typeof (MyRequest));
	///    </code>
	/// </example>
	public interface IResponder
	{
		/// <summary>
		///    Gets the request that the current instance is to respond
		///    to.
		/// </summary>
		/// <value>
		///    A <see cref="ResponderRequest" /> object containing the
		///    request that the current instance is to respond to.
		/// </value>
		ResponderRequest Request {get;}
		
		/// <summary>
		///    Processes the request and performs the response.
		/// </summary>
		/// <returns>
		///    <para>A <see cref="int" /> containing the application
		///    status the request ended with.</para>
		///    <para>This is the same value as would be returned by a
		///    program on termination. On successful termination, this
		///    would be zero.</para>
		/// </returns>
		/// <remarks>
		///    <para>In the event that the method spawns its own
		///    thread for responding to the request, a value of <see
		///    cref="int.MinValue" /> will prevent the calling method
		///    from completing the request. In that case, the thread
		///    will be responsible for calling <see
		///    cref="Request.CompleteRequest" /> with the appropriate
		///    application status and <see
		///    cref="ProtocolStatus.RequestComplete" />.</para>
		/// </remarks>
		int Process ();
	}
}