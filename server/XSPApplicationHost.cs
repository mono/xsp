//
// Mono.ASPNET.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	class Worker
	{
		IApplicationHost host;
		TcpClient client;

		public Worker (TcpClient client, IApplicationHost host)
		{
			this.client = client;
			this.host = host;
		}
		
		public void Run (object state)
		{
			XSPWorkerRequest mwr = new XSPWorkerRequest (client, host);
			mwr.ProcessRequest ();
		}
	}
	
	public class XSPApplicationHost : MarshalByRefObject, IApplicationHost
	{
		TcpListener listen_socket;
		bool started;
		bool stop;
		IPEndPoint bindAddress;
		Thread runner;
		string path;
		string vpath;

		public XSPApplicationHost ()
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
			if (started)
				throw new InvalidOperationException ("The server is already started.");

			if (bindAddress == null)
				throw new ArgumentNullException ("bindAddress");

			this.bindAddress = bindAddress;
		}

		public void Start ()
		{
			if (started)
				throw new InvalidOperationException ("The server is already started.");

			listen_socket = new TcpListener (bindAddress);
			listen_socket.Start ();
			runner = new Thread (new ThreadStart (RunServer));
			runner.Start ();
			stop = false;
			WebTrace.WriteLine ("Server started.");
		}

		public void Stop ()
		{
			if (!started)
				throw new InvalidOperationException ("The server is not started.");

			stop = true;	
			listen_socket.Stop ();
			WebTrace.WriteLine ("Server stopped.");
		}

		private void RunServer ()
		{
			started = true;
			TcpClient client;
			while (!stop){
				client = listen_socket.AcceptTcpClient ();
				WebTrace.WriteLine ("Accepted connection.");
				Worker worker = new Worker (client, this);
				ThreadPool.QueueUserWorkItem (new WaitCallback (worker.Run));
			}

			started = false;
		}

		object IApplicationHost.CreateApplicationHost (string virtualPath, string baseDirectory)
		{
			return CreateApplicationHost (virtualPath, baseDirectory);
		}

		public static object CreateApplicationHost (string virtualPath, string baseDirectory)
		{
			return ApplicationHost.CreateApplicationHost (typeof (XSPApplicationHost),
								      virtualPath,
								      baseDirectory);
		}

		public string Path {
			get {
				if (path == null)
					path = AppDomain.CurrentDomain.GetData (".appPath").ToString ();

				return path;
			}
		}

		public string VPath {
			get {
				if (vpath == null)
					vpath =  AppDomain.CurrentDomain.GetData (".appVPath").ToString ();

				return vpath;
			}
		}

	}
}

