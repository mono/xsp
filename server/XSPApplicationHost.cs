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
	class HttpErrors
	{
		static byte [] error500;

		static HttpErrors ()
		{
			string s = "HTTP/1.0 500 Server error\n\n" +
				   "<html><head><title>500 Server Error</title><body><h1>Server error</h1>\n" +
				   "Your client sent a request that was not understood by this server.\n" +
				   "</body></html>\n";
			error500 = Encoding.Default.GetBytes (s);
		}

		public static byte [] NotFound (string uri)
		{
			string s = String.Format ("<html><head><title>404 Not Found</title></head>\n" +
				"<body><h1>Not Found</h1>The requested URL {0} was not found on this " +
				"server.<p>\n</body></html>\n", uri);

			return Encoding.ASCII.GetBytes (s);
		}

		public static byte [] ServerError ()
		{
			return error500;
		}
	}

	[Serializable]
	class Worker
	{
		[NonSerialized] XSPApplicationServer server;
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

		public Worker (Socket client, EndPoint localEP, XSPApplicationServer server)
		{
			ns = new NetworkStream (client, true);
			this.server = server;
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
				host = server.GetApplicationForPath (rdata.Path, true);
				if (host == null) {
					byte [] nf = HttpErrors.NotFound (rdata.Path);
					ns.Write (nf, 0, nf.Length);
					return;
				}
#else
				RequestReader rr = new RequestReader (ns);
				host = server.GetApplicationForPath (rr.GetUriPath (), false);
				if (host == null) {
					rr.Decline ();
					return;
				}
				modRequest = rr.Request;
#endif
				CrossAppDomainDelegate pr = new CrossAppDomainDelegate (ProcessRequest);
				host.Domain.DoCallBack (pr);
			} catch (Exception e) {
				Console.WriteLine (e);
				try {
					byte [] error = HttpErrors.ServerError ();
					ns.Write (error, 0, error.Length);
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
		string path;
		string vpath;

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

	public class XSPApplicationServer
	{
		Socket listen_socket;
		bool started;
		bool stop;
#if !MODMONO_SERVER
		IPEndPoint bindAddress;
#endif
		Thread runner;
 		Hashtable appToDir;
 		Hashtable dirToHost;
 		object marker = new object ();

#if MODMONO_SERVER
		string filename;
#endif

		public XSPApplicationServer (string applications)
		{
#if MODMONO_SERVER
#else
			SetListenAddress (80);
#endif
			SetApplications (applications);
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
 
 			if (applications == "")
				return;

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
					lock (dirToHost) {
						object o = dirToHost [dir];
						if (o == marker) {
							o = CreateApplicationHost (current, dir);
							dirToHost [dir] = o;
						}
						bestFit = o as IApplicationHost;
					}
					break;
				}
				
				count--;
			}

			if (defaultToRoot && bestFit == null) {
				if (appToDir.ContainsKey ("/"))
					bestFit = dirToHost [appToDir ["/"]] as IApplicationHost;
			}

			return bestFit;
		}
	}
}

