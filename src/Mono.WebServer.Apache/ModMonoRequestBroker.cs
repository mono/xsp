//
// Mono.WebServer.ModMonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004-2007 Novell, Inc. (http://www.novell.com)
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

using Mono.WebServer.Apache;

namespace Mono.WebServer
{
	//
	// ModMonoRequestBroker: The request broker for mod_mono. Provides some
	// additional methods to the BaseRequestBroker from which inherits.
	//
	public class ModMonoRequestBroker: BaseRequestBroker
	{
		public string GetServerVariable (int requestId, string name)
		{
			var worker = GetWorker (requestId) as ModMonoWorker;
			if (worker == null)
				return null;
			return worker.GetServerVariable (name);
		}

		public void SetStatusCodeLine (int requestId, int code, string status)
		{
			var worker = GetWorker (requestId) as ModMonoWorker;
			if (worker == null)
				return;
			worker.SetStatusCodeLine (code, status);
		}
		
		public void SetResponseHeader (int requestId, string name, string value)
		{
			var worker = GetWorker (requestId) as ModMonoWorker;
			if (worker == null)
				return;
			worker.SetResponseHeader (name, value);
		}

		public void SetOutputBuffering (int requestId, bool doBuffer)
		{
			var worker = GetWorker (requestId) as ModMonoWorker;
			if (worker == null)
				return;
			worker.SetOutputBuffering (doBuffer);
		}
		
		public void SendFile (int requestId, string filename)
		{
			var worker = GetWorker (requestId) as ModMonoWorker;
			if (worker == null)
				return;
			worker.SendFile (filename);
		}

		public bool GetHeadersSent (int requestId) {
			var worker = GetWorker (requestId) as ModMonoWorker;
			return (worker != null) && worker.HeadersSent;
		}
	}
}
