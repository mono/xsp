//
// Mono.ASPNET.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace Mono.ASPNET
{
	//
	// XSPWebSource: Provides methods to get objects and types specific
	// to XSP.
	//
	public class XSPWebSource: IWebSource
	{
		IPEndPoint bindAddress;
		
		public XSPWebSource (IPAddress address, int port)
		{
			SetListenAddress (address, port);
		}
		
		public void SetListenAddress (int port)
		{
			SetListenAddress (IPAddress.Any, port);
		}

		public void SetListenAddress (IPAddress address, int port) 
		{
			SetListenAddress (new IPEndPoint (address, port));
		}
		
		public void SetListenAddress (IPEndPoint bindAddress)
		{
			if (bindAddress == null)
				throw new ArgumentNullException ("bindAddress");

			this.bindAddress = bindAddress;
		}
		
		public Socket CreateSocket ()
		{
			Socket listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (bindAddress);
			return listen_socket;
		} 
 
		public IWorker CreateWorker (Socket client, ApplicationServer server)
		{
			return new XSPWorker (client, client.LocalEndPoint, server);
		}
		
		public Type GetApplicationHostType ()
		{
			return typeof (XSPApplicationHost);
		}
		
		public IRequestBroker CreateRequestBroker ()
		{
			return new XSPRequestBroker ();
		}
	}
	
	//
	// XSPRequestBroker: The request broker for XSP. Provides some
	// additional methods to the BaseRequestBroker from which inherits.
	//
	public class XSPRequestBroker: BaseRequestBroker
	{
		public bool IsConnected (int requestId)
		{
			return ((XSPWorker)GetWorker (requestId)).IsConnected ();
		}

		public int GetReuseCount (int requestId)
		{
			XSPWorker worker = (XSPWorker) GetWorker (requestId);
			return worker.GetReuseCount ();
		}

		public void Close (int requestId, bool keepAlive)
		{
			XSPWorker worker = (XSPWorker) GetWorker (requestId);
			if (worker != null)
				worker.Close (keepAlive);
		}
	}
	
	//
	// XSPApplicationHost: The application host for XSP.
	//
	public class XSPApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (int reqId, long localEPAddr, int localEPPort, long remoteEPAdds,
					int remoteEPPort, string verb, string path, string pathInfo,
					string queryString, string protocol, byte [] inputBuffer, string redirect)
		{
			XSPRequestBroker broker = (XSPRequestBroker) RequestBroker;
			IPEndPoint localEP = new IPEndPoint (localEPAddr, localEPPort);
			IPEndPoint remoteEP = new IPEndPoint (remoteEPAdds, remoteEPPort);
			XSPWorkerRequest mwr = new XSPWorkerRequest (reqId, broker, this, localEP, remoteEP, verb, path,
								pathInfo, queryString, protocol, inputBuffer);

			string translated = mwr.GetFilePathTranslated ();
			if (path [path.Length - 1] != '/' && Directory.Exists (translated))
				redirect = path + '/';

			if (redirect != null) {
				Redirect (mwr, redirect);
				broker.UnregisterRequest (reqId);
				return;
			}

			ProcessRequest (mwr);
		}

		static string content301 = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
				"<html><head>\n<title>301 Moved Permanently</title>\n</head><body>\n" +
				"<h1>Moved Permanently</h1>\n" +
				"<p>The document has moved to <a href='http://{0}{1}'>http://{0}{1}</a>.</p>\n" +
				"</body></html>\n";

		static void Redirect (XSPWorkerRequest wr, string location)
		{
			string host = wr.GetKnownRequestHeader (HttpWorkerRequest.HeaderHost);
			wr.SendStatus (301, "Moved Permanently");
			wr.SendUnknownResponseHeader ("Connection", "close");
			wr.SendUnknownResponseHeader ("Date", DateTime.Now.ToUniversalTime ().ToString ("r"));
			wr.SendUnknownResponseHeader ("Location", String.Format ("http://{0}{1}", host, location));
			Encoding enc = Encoding.ASCII;
			wr.SendUnknownResponseHeader ("Content-Type", "text/html; charset=" + enc.WebName);
			string content = String.Format (content301, host, location);
			byte [] contentBytes = enc.GetBytes (content);
			wr.SendUnknownResponseHeader ("Content-Length", contentBytes.Length.ToString ());
			wr.SendResponseFromMemory (contentBytes, contentBytes.Length);
			wr.FlushResponse (true);
			wr.CloseConnection ();
		}

		static string GetValueFromHeader (string header, string attr)
		{
			int where = header.IndexOf (attr + '=');
			if (where == -1)
				return null;

			where += attr.Length + 1;
			int max = header.Length;
			if (where >= max)
				return String.Empty;

			char ending = header [where];
			if (ending != '"')
				ending = ' ';

			int end = header.Substring (where + 1).IndexOf (ending);
			if (end == -1)
				return (ending == '"') ? null : header.Substring (where);

			return header.Substring (where, end);
		}
	}
	
	//
	// XSPWorker: The worker that do the initial processing of XSP
	// requests.
	//
	internal class XSPWorker: IWorker
	{
		ApplicationServer server;
		LingeringNetworkStream stream;
		IPEndPoint remoteEP;
		IPEndPoint localEP;
		Socket sock;

		public XSPWorker (Socket client, EndPoint localEP, ApplicationServer server)
		{
			stream = new LingeringNetworkStream (client, false);
			sock = client;
			this.server = server;

			try {
				remoteEP = (IPEndPoint) client.RemoteEndPoint;
			} catch { }
			this.localEP = (IPEndPoint) localEP;
		}

		public int GetReuseCount ()
		{
			return server.GetAvailableReuses (sock);
		}

		public void Run (object state)
		{
			int requestId = -1;
			XSPRequestBroker broker = null;
			
			try {
				if (remoteEP == null)
					return;

				InitialWorkerRequest ir = new InitialWorkerRequest (stream);
				try {
					ir.ReadRequestData ();
				} catch (Exception ex) {
					if (ir.GotSomeInput) {
						byte [] badReq = HttpErrors.BadRequest ();
						Write (badReq, 0, badReq.Length);
					}

					throw ex;
				}

				RequestData rdata = ir.RequestData;
				string vhost = null; // TODO: read the headers in InitialWorkerRequest
				int port = ((IPEndPoint) localEP).Port;
				
				VPathToHost vapp = server.GetApplicationForPath (vhost, port, rdata.Path, true);
				XSPApplicationHost host = null;
				if (vapp != null)
					host = (XSPApplicationHost) vapp.AppHost;

				if (host == null) {
					byte [] nf = HttpErrors.NotFound (rdata.Path);
					Write (nf, 0, nf.Length);
					Close ();
					return;
				}
				
				broker = (XSPRequestBroker) vapp.RequestBroker;
				requestId = broker.RegisterRequest (this);
				
				string redirect;
				vapp.Redirect (rdata.Path, out redirect);
				host.ProcessRequest (requestId, localEP.Address.Address, localEP.Port,
						remoteEP.Address.Address, remoteEP.Port, rdata.Verb,
						rdata.Path, rdata.PathInfo, rdata.QueryString,
						rdata.Protocol, rdata.InputBuffer, redirect);
			} catch (Exception e) {
				bool ignore = ((e is RequestLineException) || (e is IOException));
				if (!ignore)
					Console.WriteLine (e);

				try {
					if (!ignore) {
						byte [] error = HttpErrors.ServerError ();
						Write (error, 0, error.Length);
					}
				} catch {}
				try {
					Close ();
				} catch {}

				if (broker != null && requestId != -1)
					broker.UnregisterRequest (requestId);
			}
		}

		public int Read (byte[] buffer, int position, int size)
		{
			return stream.Read (buffer, position, size);
		}
		
		public void Write (byte[] buffer, int position, int size)
		{
			stream.Write (buffer, position, size);
		}
		
		public void Close ()
		{
			Close (false);
		}

		public void Close (bool keepAlive)
		{
			if (!keepAlive || !IsConnected ()) {
				stream.Close ();
				server.CloseSocket (sock);
				return;
			}

			stream.EnableLingering = false;
			stream.Close ();
			server.ReuseSocket (sock);
		}

		public bool IsConnected ()
		{
			return stream.Connected;
		}

		public void Flush ()
		{
			stream.Flush ();
		}
	}
}
