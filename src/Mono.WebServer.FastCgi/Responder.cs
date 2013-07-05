//
// Responder.cs: Resonds to FastCGI "Responder" requests with an ASP.NET
// application.
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
using System.Collections.Generic;
using Mono.FastCgi;
using Mono.WebServer.Log;

namespace Mono.WebServer.FastCgi
{
	public class Responder : MarshalByRefObject, IResponder
	{
		const string ERROR500 =
			"Status: 500 Internal Server Error\r\n" +
			"Content-Type: text/html; charset=utf-8\r\n" +
			"Connection: close\r\n\r\n" +
			"<html>\r\n" +
			"	<head>\r\n" +
			"		<title>500 No Application Found</title>\r\n" +
			"	</head>\r\n" +
			"	<body>\r\n" +
			"		<h1>No Application Found</h1>\r\n" +
			"		<p>Unable to find a matching application for request:</p>\r\n" +
			"		<table>\r\n" +
			"			<tr><th>Host</th><td>{0}</td>\r\n" +
			"			<tr><th>Port</th><td>{1}</td>\r\n" +
			"			<tr><th>Request Path</th><td>{2}</td>\r\n" +
			"			<tr><th>Physical Path</th><td>{3}</td>\r\n" +
			"		</table>\r\n" +
			"	</body>\r\n" +
			"</html>\r\n";
		
		readonly ResponderRequest request;
		
		public Responder (ResponderRequest request)
		{
			this.request = request;
		}
		
		public int Process ()
		{
			// Uncommenting the following lines will cause the page
			// + headers to be rendered as plain text. (Pretty sweet
			// for debugging.)
		/*	
			request.SendOutputText ("Content-type: text/plain\r\n\r\n");
			request.SendOutputText ("Output:\r\n");
			request.SendOutputText (Path + "\r\n");
			request.SendOutputText (PhysicalPath + "\r\n");
		*/

			ApplicationHost appHost = request.ApplicationHost;
			
			// If the application host is null, the server was
			// unable to determine a sane plan. Alert the client.
			if (appHost == null) {
				request.SendOutputText (String.Format (ERROR500,
					HostName, PortNumber,
					Path, PhysicalPath));
				return -1;
			}
			
			try {
				appHost.ProcessRequest (this);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error,
					"ERROR PROCESSING REQUEST: " + e);
				return -1;
			}
			
			// MIN_VALUE means don't close.
			return Int32.MinValue;
		}
		
		public ResponderRequest Request {
			get {return request;}
		}
		
		public void SendOutput(string text, System.Text.Encoding encoding)
		{
			request.SendOutput (text, encoding);
		}
		
		public void SendOutput (byte [] data, int length)
		{
			request.SendOutput (data, length);
		}
		
		public string GetParameter (string name)
		{
			return request.GetParameter (name);
		}
		
		public IDictionary<string,string> GetParameters ()
		{
			return request.GetParameters ();
		}
		
		public int RequestID {
			get {return request.RequestID;}
		}
		
		public byte [] InputData {
			get {return request.InputData;}
		}
		
		public void CompleteRequest (int appStatus)
		{
			request.CompleteRequest (appStatus, ProtocolStatus.RequestComplete);
		}
		
		public bool IsConnected {
			get {return request.IsConnected;}
		}
		
		public string HostName {
			get {return request.HostName;}
		}
		
		public int PortNumber {
			get {return request.PortNumber;}
		}
		
		public string Path {
			get {return request.Path;}
		}
		
		public string PhysicalPath {
			get {return request.PhysicalPath;}
		}
	}
}
