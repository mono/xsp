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
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
#if MODMONO_SERVER
using Mono.Posix;
#endif

namespace Mono.ASPNET
{
	class Worker
	{
		IApplicationHost host;
		Socket client;
		static byte [] error500;

		static Worker ()
		{
			string s = "HTTP/1.0 500 Server error\n\n" +
				   "<html><body><h1>500 Server error</h1>\n" +
				   "Your client sent a request that was not understood by this server.\n" +
				   "</body></html>\n";
			
			error500 = Encoding.Default.GetBytes (s);
		}
		
		public Worker (Socket client, IApplicationHost host)
		{
			this.client = client;
			this.host = host;
		}
		
		public void Run (object state)
		{
			try {
				XSPWorkerRequest mwr = new XSPWorkerRequest (client, host);
				mwr.ProcessRequest ();
			} catch (Exception e) {
				Console.WriteLine (e);
				try {
					NetworkStream ns = new NetworkStream (client, false);
					ns.Write (error500, 0, error500.Length);
					ns.Close ();
					client.Close ();
				} catch {}
			}
		}
	}
	
	public class XSPApplicationHost : MarshalByRefObject, IApplicationHost
	{
		Socket listen_socket;
		bool started;
		bool stop;
		IPEndPoint bindAddress;
		Thread runner;
		string path;
		string vpath;
#if MODMONO_SERVER
		string filename;
#endif

		public XSPApplicationHost ()
		{
#if MODMONO_SERVER
#else
			SetListenAddress (80);
#endif
		}

#if MODMONO_SERVER
		public void SetListenFile (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");

			this.filename = filename;
		}
#else
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
#endif
		public void Start ()
		{
			if (started)
				throw new InvalidOperationException ("The server is already started.");

#if MODMONO_SERVER
			if (filename == null)
				throw new InvalidOperationException ("filename not set");
				
			File.Delete (filename);
			listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			EndPoint ep = new UnixEndPoint (filename);
			listen_socket.Bind (ep);
#else
			listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			listen_socket.Bind (bindAddress);
#endif
			listen_socket.Listen (5);
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
			listen_socket.Close ();
			WebTrace.WriteLine ("Server stopped.");
		}

		private void RunServer ()
		{
			started = true;
			Socket client;
			while (!stop){
				client = listen_socket.Accept ();
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

