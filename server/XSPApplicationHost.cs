//
// Mono.ASPNET.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
//
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Runtime.Remoting.Lifetime;
#if MODMONO_SERVER
using Mono.Posix;
#endif

namespace Mono.ASPNET
{
	[Serializable]
	class Worker
	{
		IApplicationHost host;
		NetworkStream ns;
		bool requestFinished;
#if MODMONO_SERVER
		ModMonoRequest modRequest;
#else
		RequestData rdata;
		EndPoint remoteEP;
		EndPoint localEP;
#endif

		static byte [] error500;

		static Worker ()
		{
			string s = "HTTP/1.0 500 Server error\n\n" +
				   "<html><body><h1>500 Server error</h1>\n" +
				   "Your client sent a request that was not understood by this server.\n" +
				   "</body></html>\n";
			
			error500 = Encoding.Default.GetBytes (s);
		}
		
		public Worker (Socket client, EndPoint localEP, IApplicationHost host)
		{
			ns = new NetworkStream (client, true);
			this.host = host;
#if !MODMONO_SERVER
			try {
				remoteEP = client.RemoteEndPoint;
			} catch { }
			this.localEP = localEP;
#endif
		}

		public void Run (object state)
		{
			try {
#if !MODMONO_SERVER
				if (remoteEP == null)
					return;

				InitialWorkerRequest ir = new InitialWorkerRequest (ns);
				ir.ReadRequestData ();
				rdata = ir.RequestData;
				host = host.GetApplicationForPath (rdata.Path, true);
#else
				XSPWorkerRequest mwr = new XSPWorkerRequest (ns, host);
				mwr.ReadRequestData ();
				host = host.GetApplicationForPath (mwr.GetUriPath (), false);
				if (host == null) {
					mwr.Decline ();
					return;
				}
				modRequest = mwr.Request;
#endif
				CrossAppDomainDelegate pr = new CrossAppDomainDelegate (ProcessRequest);
				host.Domain.DoCallBack (pr);
			} catch (Exception e) {
				Console.WriteLine (e);
				try {
					ns.Write (error500, 0, error500.Length);
				} catch {}
			} finally {
				requestFinished = true;
				try {
					ns.Close ();
				} catch {}
			}
		}

		public void ProcessRequest ()
		{
#if !MODMONO_SERVER
			XSPWorkerRequest mwr = new XSPWorkerRequest (ns, host, localEP, remoteEP, rdata);
#else
			XSPWorkerRequest mwr = new XSPWorkerRequest (modRequest, host);
#endif
			if (!mwr.ReadRequestData ()) {
				requestFinished = true;
				return;
			}

			mwr.ProcessRequest ();
		}
	}

	public class XSPApplicationHost : MarshalByRefObject, IApplicationHost
	{
		Socket listen_socket;
		bool started;
		bool stop;
#if !MODMONO_SERVER
		IPEndPoint bindAddress;
#endif
		Thread runner;
		string path;
		string vpath;
 		Hashtable appToDir;
 		Hashtable dirToHost;
 		object marker = new object ();

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

 		public void SetApplications (string applications)
 		{
 			if (applications == null)
 				throw new ArgumentNullException ("applications");
 
 			if (appToDir != null)
 				throw new InvalidOperationException ("Already called");
 
 			appToDir = new Hashtable ();
 			dirToHost = new Hashtable ();
 			string [] apps = applications.Split (',');
			//Console.WriteLine ("applications: {0}", applications);
 			foreach (string str in apps) {
 				string [] app = str.Split (':');
 				if (app.Length != 2)
 					throw new ArgumentException ("Should be something like VPath:realpath");
 
 				string fp = System.IO.Path.GetFullPath (app [1]);
 				//Console.WriteLine ("{0} -> {1}", app [0], fp);
 				appToDir [app [0]] = fp;
 				dirToHost [fp] = marker;
 			}
 		}
 
		public void Start ()
		{
			if (started)
				throw new InvalidOperationException ("The server is already started.");

 			if (appToDir == null)
 				throw new InvalidOperationException ("SetApplications must be called first");

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
			runner.IsBackground = true;
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
			dirToHost = new Hashtable ();
			WebTrace.WriteLine ("Server stopped.");
		}

		private void RunServer ()
		{
			started = true;
			Socket client;
			while (!stop){
				client = listen_socket.Accept ();
				WebTrace.WriteLine ("Accepted connection.");
				Worker worker = new Worker (client, client.LocalEndPoint, this);
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

		public IApplicationHost GetApplicationForPath (string path, bool defaultToRoot)
		{
			IApplicationHost bestFit = null;
			string [] parts = path.Split ('/');
			int count = parts.Length;
			while (count > 0) {
				string current = String.Join ("/", parts, 0, count);
				if (current == "")
					current = "/";

				//Console.WriteLine ("current: {0} path: {1}", current, path);
				string dir = appToDir [current] as string;
				if (dir != null) {
					object o = dirToHost [dir];
					bestFit = o as IApplicationHost;
					if (o == marker) {
						o = CreateApplicationHost (current, dir);
						dirToHost [dir] = o;
						bestFit = (IApplicationHost) o;
					}
					break;
				} else {
					//Console.WriteLine ("null for {0}", current);
				}
				
				count--;
			}

			if (defaultToRoot && bestFit == null) {
				//Console.WriteLine ("bestFit es null 1");
				bestFit = dirToHost [appToDir ["/"]] as IApplicationHost;
				/*
				if (bestFit == null) {
					Console.WriteLine ("bestFit es null 2");
					foreach (string key in dirToHost.Keys)
						Console.WriteLine ("Key: {0} Value: '{1}'", key, dirToHost [key]);
				}
				*/
			}

			return bestFit;
		}
		
		public override object InitializeLifetimeService ()
		{
			return null; // who wants to live forever?
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

		public AppDomain Domain {
			get { return AppDomain.CurrentDomain; }
		}
	}
}

