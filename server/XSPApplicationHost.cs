//
// Mono.ASPNET.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
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
	}
	
	//
	// XSPApplicationHost: The application host for XSP.
	//
	public class XSPApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (int reqId, string localEPAddr, int localEPPort, string remoteEPAdds,
					int remoteEPPort, string verb, string path, string pathInfo,
					string queryString, string protocol, byte [] inputBuffer, string redirect)
		{
			XSPRequestBroker broker = (XSPRequestBroker) RequestBroker;
			IPEndPoint localEP = new IPEndPoint (IPAddress.Parse (localEPAddr), localEPPort);
			IPEndPoint remoteEP = new IPEndPoint (IPAddress.Parse (remoteEPAdds), remoteEPPort);
			XSPWorkerRequest mwr = new XSPWorkerRequest (reqId, broker, this, localEP, remoteEP, verb, path,
								pathInfo, queryString, protocol, inputBuffer);

			if (redirect != null) {
				Redirect (mwr, redirect);
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
		RequestData rdata;
		IPEndPoint remoteEP;
		IPEndPoint localEP;

		public XSPWorker (Socket client, EndPoint localEP, ApplicationServer server)
		{
			stream = new LingeringNetworkStream (client, true);
			this.server = server;

			try {
				remoteEP = (IPEndPoint) client.RemoteEndPoint;
			} catch { }
			this.localEP = (IPEndPoint) localEP;
		}

		public void Run (object state)
		{
			int requestId = -1;
			XSPRequestBroker broker = null;
			
			try {
				if (remoteEP == null)
					return;

				InitialWorkerRequest ir = new InitialWorkerRequest (stream);
				ir.ReadRequestData ();
				RequestData rdata = ir.RequestData;
				string vhost = null; // TODO: read the headers in InitialWorkerRequest
				int port = ((IPEndPoint) localEP).Port;
				
				VPathToHost vapp = server.GetApplicationForPath (vhost, port, rdata.Path, true);
				XSPApplicationHost host = null;
				if (vapp != null)
					host = (XSPApplicationHost) vapp.AppHost;

				if (host == null) {
					byte [] nf = HttpErrors.NotFound (rdata.Path);
					stream.Write (nf, 0, nf.Length);
					stream.Close ();
					return;
				}
				
				broker = (XSPRequestBroker) vapp.RequestBroker;
				requestId = broker.RegisterRequest (this);
				
				string redirect;
				vapp.Redirect (rdata.Path, out redirect);
				host.ProcessRequest (requestId, localEP.Address.ToString(), localEP.Port,
						remoteEP.Address.ToString(), remoteEP.Port, rdata.Verb,
						rdata.Path, rdata.PathInfo, rdata.QueryString,
						rdata.Protocol, rdata.InputBuffer, redirect);
			} catch (Exception e) {
				Console.WriteLine (e);
				try {
					if (!(e is IOException)) {
						byte [] error = HttpErrors.ServerError ();
						stream.Write (error, 0, error.Length);
					}
					stream.Close ();
				} catch {}
			} finally {
				if (requestId != -1)
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
			stream.Close ();
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
