//
// Mono.ASPNET.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//  Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//

using System;
using System.Net;
using System.Net.Sockets;

namespace Mono.ASPNET
{
	//
	// XSPWebSource: Provides methods to get objects and types specific
	// to XSP.
	//
	public class XSPWebSource: IWebSource
	{
		IPEndPoint bindAddress;
		
		public XSPWebSource ()
		{
			SetListenAddress (80);
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
		public void ProcessRequest (int reqId, string localEPAddr, int localEPPort, string remoteEPAdds, int remoteEPPort, string verb, string path, string pathInfo, string queryString, string protocol, byte[] inputBuffer)
		{
			XSPRequestBroker broker = (XSPRequestBroker) RequestBroker;
			IPEndPoint localEP = new IPEndPoint (IPAddress.Parse(localEPAddr), localEPPort);
			IPEndPoint remoteEP = new IPEndPoint (IPAddress.Parse(remoteEPAdds), remoteEPPort);
			XSPWorkerRequest mwr = new XSPWorkerRequest (reqId, broker, this, localEP, remoteEP, verb, path, pathInfo, queryString, protocol, inputBuffer);
			ProcessRequest (mwr);
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
				XSPApplicationHost host = (XSPApplicationHost) vapp.AppHost;
				if (host == null) {
					byte [] nf = HttpErrors.NotFound (rdata.Path);
					stream.Write (nf, 0, nf.Length);
					stream.Close ();
					return;
				}
				
				broker = (XSPRequestBroker) vapp.RequestBroker;
				requestId = broker.RegisterRequest (this);
				
				host.ProcessRequest (requestId, localEP.Address.ToString(), localEP.Port, remoteEP.Address.ToString(), remoteEP.Port, rdata.Verb, rdata.Path, rdata.PathInfo, rdata.QueryString, rdata.Protocol, rdata.InputBuffer);

			} catch (Exception e) {
				Console.WriteLine (e);
				try {
					byte [] error = HttpErrors.ServerError ();
					stream.Write (error, 0, error.Length);
					stream.Close ();
				} catch {}
			}
			finally {
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
