//
// Mono.ASPNET.MonoApplicationHost
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

namespace Mono.ASPNET
{
	class Worker
	{
		MonoApplicationHost host;
		TcpClient client;

		public Worker (TcpClient client, MonoApplicationHost host)
		{
			this.client = client;
			this.host = host;
		}
		
		public void Run (object state)
		{
			MonoWorkerRequest mwr = new MonoWorkerRequest (client, host);
			mwr.ProcessRequest ();
		}
	}
	
	public class MonoApplicationHost : MarshalByRefObject
	{
		TcpListener listen_socket;
		bool started;
		bool stop;
		IPEndPoint bindAddress;
		Thread runner;

		public MonoApplicationHost ()
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
		
		public string Path
		{
			get {
				return AppDomain.CurrentDomain.GetData (".appPath").ToString ();
			}
		}

		public string VPath
		{
			get {
				return AppDomain.CurrentDomain.GetData (".appVPath").ToString ();
			}
		}

	}
}

