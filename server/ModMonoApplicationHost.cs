//
// Mono.ASPNET.ModMonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//  Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Mono.Posix;

namespace Mono.ASPNET
{
	//
	// ModMonoWebSource: Provides methods to get objects and types specific
	// to mod_mono.
	//
	public class ModMonoWebSource: IWebSource
	{
		string filename;
		
		public void SetListenFile (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");

			this.filename = filename;
		}
		
		public Socket CreateSocket ()
		{
			if (filename == null)
				throw new InvalidOperationException ("filename not set");

			File.Delete (filename);
			Socket listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			EndPoint ep = new UnixEndPoint (filename);
			listen_socket.Bind (ep);
			return listen_socket;
		}
 
		public IWorker CreateWorker (Socket client, ApplicationServer server)
		{
			return new ModMonoWorker (client, server);
		}
		
		public Type GetApplicationHostType ()
		{
			return typeof (ModMonoApplicationHost);
		}
		
		public IRequestBroker CreateRequestBroker ()
		{
			return new ModMonoRequestBroker ();
		}
	}
	
	//
	// ModMonoRequestBroker: The request broker for mod_mono. Provides some
	// additional methods to the BaseRequestBroker from which inherits.
	//
	public class ModMonoRequestBroker: BaseRequestBroker
	{
		public string GetServerVariable (int requestId, string name)
		{
			return ((ModMonoWorker)GetWorker (requestId)).GetServerVariable (name);
		}

		public void SetStatusCodeLine (int requestId, int code, string status)
		{
			((ModMonoWorker)GetWorker (requestId)).SetStatusCodeLine (code, status);
		}
		
		public void SetResponseHeader (int requestId, string name, string value)
		{
			((ModMonoWorker)GetWorker (requestId)).SetResponseHeader (name, value);
		}
	}

	//
	// ModMonoApplicationHost: The application host for mod_mono.
	//
	public class ModMonoApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (int reqId, string verb, string queryString, string path,
					string protocol, string localAddress, int serverPort, string remoteAddress,
					int remotePort, string remoteName, string [] headers, string [] headerValues)
		{
			ModMonoRequestBroker broker = (ModMonoRequestBroker) RequestBroker;
			ModMonoWorkerRequest mwr = new ModMonoWorkerRequest (reqId, broker, this, verb, path, queryString,
									protocol, localAddress, serverPort, remoteAddress,
									remotePort, remoteName, headers, headerValues);

			ProcessRequest (mwr);
		}
	}
	
	//
	// ModMonoWorker: The worker that do the initial processing of mod_mono
	// requests.
	//
	internal class ModMonoWorker: IWorker
	{
		ApplicationServer server;
		public LingeringNetworkStream Stream;
		ModMonoRequest modRequest;

		public ModMonoWorker (Socket client, ApplicationServer server)
		{
			Stream = new LingeringNetworkStream (client, true);
			Stream.EnableLingering = false;
			this.server = server;
		}

		public void Run (object state)
		{
			int requestId = -1;
			ModMonoRequestBroker broker = null;
			
			try {
				RequestReader rr = new RequestReader (Stream);
				string vhost = rr.Request.GetRequestHeader ("Host");
				int port = -1;
				if (vhost != null) {
					int colon = vhost.IndexOf (':');
					if (colon != -1) {
						port = Int32.Parse (vhost.Substring (colon + 1));
						vhost = vhost.Substring (0, colon);
					} else {
						port = 80;
					}
				}
				
				VPathToHost vapp = server.GetApplicationForPath (vhost, port, rr.GetUriPath (), false);
				ModMonoApplicationHost host = (ModMonoApplicationHost) vapp.AppHost;
				if (host == null) {
					rr.Decline ();
					return;
				}
				modRequest = rr.Request;
				
				broker = (ModMonoRequestBroker) vapp.RequestBroker; 
				requestId = broker.RegisterRequest (this);
				
				host.ProcessRequest (requestId, 
									modRequest.GetHttpVerbName(), 
									modRequest.GetQueryString(), 
									modRequest.GetUri(), 
									modRequest.GetProtocol(), 
									modRequest.GetLocalAddress(), 
									modRequest.GetServerPort(), 
									modRequest.GetRemoteAddress(), 
									modRequest.GetRemotePort(), 
									modRequest.GetRemoteName(), 
									modRequest.GetAllHeaders(),
									modRequest.GetAllHeaderValues());
				
			} catch (Exception e) {
				Console.WriteLine (e);
				try {
					byte [] error = HttpErrors.ServerError ();
					Stream.Write (error, 0, error.Length);
					Stream.Close ();
				} catch {}
			}
			finally {
				if (requestId != -1)
					broker.UnregisterRequest (requestId);
			}
		}

		public int Read (byte[] buffer, int position, int size)
		{
			return modRequest.GetClientBlock (buffer, position, size);
		}
		
		public void Write (byte[] buffer, int position, int size)
		{
			modRequest.SendResponseFromMemory (buffer, position, size);
		}
		
		public void Close ()
		{
			modRequest.Close ();
		}
		
		public void Flush ()
		{
			modRequest.Flush ();
		}
		
		public string GetServerVariable (string name)
		{
			return modRequest.GetServerVariable (name);
		}
		
		public void SetStatusCodeLine (int code, string status)
		{
			modRequest.SetStatusCodeLine (code, status);
		}
		
		public void SetResponseHeader (string name, string value)
		{
			modRequest.SetResponseHeader (name, value);
		}
	}	
}
