//
// Mono.ASPNET.ApplicationServer
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//


using System;
using System.Net.Sockets;
using System.Xml;
using System.Web;
using System.Web.Hosting;
using System.Collections;
using System.Text;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mono.ASPNET
{
	// ApplicationServer runs the main server thread, which accepts client 
	// connections and forwards the requests to the correct web application.
	// ApplicationServer takes an IWebSource object as parameter in the 
	// constructor. IWebSource provides methods for getting some objects
	// whose behavior is specific to XSP or mod_mono.
	
	// Each web application lives in its own application domain, and incoming
	// requests are processed in the corresponding application domain.
	// Since the client Socket can't be passed from one domain to the other, the
	// flow of information must go through the cross-app domain channel.
	 
	// For each application two objects are created:
	// 1) a IApplicationHost object is created in the application domain
	// 2) a IRequestBroker is created in the main domain.
	//
	// The IApplicationHost is used by the ApplicationServer to start the
	// processing of a request in the application domain.
	// The IRequestBroker is used from the application domain to access 
	// information in the main domain.
	//
	// The complete sequence of servicing a request is the following:
	//
	// 1) The listener accepts an incoming connection.
	// 2) An IWorker object is created (through the IWebSource), and it is
	//    queued in the thread pool.
	// 3) When the IWorker's run method is called, it registers itself in
	//    the application's request broker, and gets a request id. All this is
	//    done in the main domain.
	// 4) The IWorker starts the request processing by making a cross-app domain
	//    call to the application host. It passes as parameters the request id
	//    and other information already read from the request.
	// 5) The application host executes the request. When it needs to read or
	//    write request data, it performs remote calls to the request broker,
	//    passing the request id provided by the IWorker.
	// 6) When the request broker receives a call from the application host,
	//    it locates the IWorker registered with the provided request id and
	//    forwards the call to it.
	
	public class ApplicationServer : MarshalByRefObject
	{
		IWebSource webSource;
		bool started;
		bool stop;
		bool verbose;
		Socket listen_socket;

		Thread runner;

		// This is much faster than hashtable for typical cases.
		ArrayList vpathToHost = new ArrayList ();
		
		public ApplicationServer (IWebSource source)
		{
			webSource = source;
		} 

		public bool Verbose {
			get { return verbose; }
			set { verbose = value; }
		}

		private void AddApplication (string vhost, int vport, string vpath, string fullPath)
		{
			// TODO - check for duplicates, sort, optimize, etc.
			if (started)
				throw new InvalidOperationException ("The server is already started.");

			if (verbose) {
				Console.WriteLine("Registering application:");
				Console.WriteLine("    Host:          {0}", (vhost != null) ? vhost : "any");
				Console.WriteLine("    Port:          {0}", (vport != -1) ?
									     vport.ToString () : "any");

				Console.WriteLine("    Virtual path:  {0}", vpath);
				Console.WriteLine("    Physical path: {0}", fullPath);
			}

			vpathToHost.Add (new VPathToHost (vhost, vport, vpath, fullPath));
		}

 		public void AddApplicationsFromConfigDirectory (string directoryName)
 		{
			if (verbose) {
				Console.WriteLine ("Adding applications from *.webapp files in " +
						   "directory '{0}'", directoryName);
			}

			DirectoryInfo di = new DirectoryInfo (directoryName);
			foreach (FileInfo fi in di.GetFiles ("*.webapp")) {
				AddApplicationsFromConfigFile (fi.FullName);
			}
		}

 		public void AddApplicationsFromConfigFile (string fileName)
 		{
			if (verbose) {
				Console.WriteLine ("Adding applications from config file '{0}'", fileName);
			}

			try {
				XmlDocument doc = new XmlDocument ();
				doc.Load (fileName);

				foreach (XmlElement el in doc.SelectNodes ("//web-application")) {
					AddApplicationFromElement (el);
				}
			} catch {
				Console.WriteLine ("Error loading '{0}'", fileName);
				throw;
			}
		}

		void AddApplicationFromElement (XmlElement el)
		{
			XmlNode n;

			string vpath = el.SelectSingleNode ("vpath").InnerText;
			string path = el.SelectSingleNode ("path").InnerText;

			string vhost = null;
			n = el.SelectSingleNode ("vhost");
#if !MOD_MONO_SERVER
			if (n != null)
				vhost = n.InnerText;
#else
			// TODO: support vhosts in xsp.exe
			string name = el.SelectSingleNode ("name").InnerText;
			if (verbose)
				Console.WriteLine ("Ignoring vhost {0} for {1}", n.InnerText, name);
#endif

			int vport = -1;
			n = el.SelectSingleNode ("vport");
#if !MOD_MONO_SERVER
			if (n != null)
				vport = Convert.ToInt32 (n.InnerText);
#else
			// TODO: Listen on different ports
			if (verbose)
				Console.WriteLine ("Ignoring vport {0} for {1}", n.InnerText, name);
#endif

			AddApplication (vhost, vport, vpath, path);
		}

 		public void AddApplicationsFromCommandLine (string applications)
 		{
 			if (applications == null)
 				throw new ArgumentNullException ("applications");
 
 			if (applications == "")
				return;

			if (verbose) {
				Console.WriteLine("Adding applications '{0}'...", applications);
			}

 			string [] apps = applications.Split (',');

			foreach (string str in apps) {
				string [] app = str.Split (':');

				if (app.Length < 2 || app.Length > 4)
					throw new ArgumentException ("Should be something like " +
								"[[hostname:]port:]VPath:realpath");

				int vport;
				string vhost;
				string vpath;
				string realpath;
				int pos = 0;

				if (app.Length >= 3) {
					vhost = app[pos++];
				} else {
					vhost = null;
				}

				if (app.Length >= 4) {
					// FIXME: support more than one listen port.
					vport = Convert.ToInt16 (app[pos++]);
				} else {
					vport = -1;
				}

				vpath = app [pos++];
				realpath = app[pos++];

				if (!vpath.EndsWith ("/"))
					vpath += "/";
 
 				string fullPath = System.IO.Path.GetFullPath (realpath);
				AddApplication (vhost, vport, vpath, fullPath);
 			}
 		}
 
		public bool Start (bool bgThread)
		{
			if (started)
				throw new InvalidOperationException ("The server is already started.");

 			if (vpathToHost == null)
 				throw new InvalidOperationException ("SetApplications must be called first.");

 			if (vpathToHost.Count == 0)
 				throw new InvalidOperationException ("No applications defined.");
 				
			listen_socket = webSource.CreateSocket ();
			listen_socket.Listen (500);
			listen_socket.Blocking = false;
			runner = new Thread (new ThreadStart (RunServer));
			runner.IsBackground = bgThread;
			runner.Start ();
			stop = false;
			WebTrace.WriteLine ("Server started.");
			return true;
		}

		public void Stop ()
		{
			if (!started)
				throw new InvalidOperationException ("The server is not started.");

			stop = true;	
			runner.Abort ();
			listen_socket.Close ();
			UnloadAll ();
			WebTrace.WriteLine ("Server stopped.");
			Environment.Exit (0);
		}

		public void UnloadAll ()
		{
			lock (vpathToHost) {
				foreach (VPathToHost v in vpathToHost) {
					v.UnloadHost ();
				}
			}
		}

		void SetSocketOptions (Socket sock)
		{
#if !MOD_MONO_SERVER
			try {
				sock.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 15000); // 15s
				sock.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 15000); // 15s
			} catch {
				// Ignore exceptions here for systems that do not support these options.
			}
#endif
		}

		ArrayList allSockets = new ArrayList ();
		ArrayList wSockets = new ArrayList ();
		Hashtable timeouts = new Hashtable ();

		void RunServer ()
		{
			allSockets.Add (listen_socket);
			wSockets.Add (listen_socket);
			started = true;
			Socket client;
			int w;
			while (!stop){
				w = wSockets.Count;
				Socket.Select (wSockets, null, null, (w == 1) ? -1 : 1000 * 1000); // 1s
				w = wSockets.Count;
				for (int i = 0; i < w; i++) {
					Socket s = (Socket) wSockets [i];
					if (s == listen_socket) {
						try {
							client = s.Accept ();
						} catch (Exception e) {
							continue;
						}
						WebTrace.WriteLine ("Accepted connection.");
						SetSocketOptions (client);
						allSockets.Add (client);
						timeouts [client] = DateTime.UtcNow;
						continue;
					}

					allSockets.Remove (s);
					timeouts.Remove (s);
					IWorker worker = webSource.CreateWorker (s, this);
					ThreadPool.QueueUserWorkItem (new WaitCallback (worker.Run));
				}

				w = timeouts.Count;
				if (w > 0) {
					Socket [] socks_timeout = new Socket [w];
					timeouts.Keys.CopyTo (socks_timeout, 0);
					DateTime now = DateTime.UtcNow;
					foreach (Socket k in socks_timeout) {
						DateTime atime = (DateTime) timeouts [k];
						TimeSpan diff = now - atime;
						if (diff.TotalMilliseconds > 15 * 1000) {
							k.Close ();
							allSockets.Remove (k);
							timeouts.Remove (k);
							continue;
						}
					}
				}
				
				wSockets.Clear ();
				if (allSockets.Count == 1) {
					// shortcut, no foreach
					wSockets.Add (listen_socket);
				} else {
					wSockets.AddRange (allSockets);
				}
			}

			started = false;
		}

		public VPathToHost GetApplicationForPath (string vhost, int port, string path,
							       bool defaultToRoot)
		{
			VPathToHost bestMatch = null;
			int bestMatchLength = 0;

			// Console.WriteLine("GetApplicationForPath({0},{1},{2},{3})", vhost, port,
			//			path, defaultToRoot);
			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				VPathToHost v = (VPathToHost) vpathToHost [i];
				int matchLength = v.vpath.Length;
				if (matchLength <= bestMatchLength || !v.Match (vhost, port, path))
					continue;

				bestMatchLength = matchLength;
				bestMatch = v;
			}

			if (bestMatch != null) {
				lock (bestMatch) {
					if (bestMatch.AppHost == null)
						bestMatch.CreateHost (this, webSource);
				}
				return bestMatch;
			}
			
			if (defaultToRoot)
				return GetApplicationForPath (vhost, port, "/", false);

			if (verbose)
				Console.WriteLine ("No application defined for: {0}:{1}{2}", vhost, port, path);

			return null;
		}

		public void DestroyHost (IApplicationHost host)
		{
			// Called when the host appdomain is being unloaded
			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				VPathToHost v = (VPathToHost) vpathToHost [i];
				if (v.TryClearHost (host))
					break;
			}
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
	
	public class VPathToHost
	{
		public readonly string vhost;
		public readonly int vport;
		public readonly string vpath;
		public readonly string realPath;
		public readonly bool haveWildcard;

		public IApplicationHost AppHost;
		public IRequestBroker RequestBroker;

		public VPathToHost (string vhost, int vport, string vpath, string realPath)
		{
			this.vhost = (vhost != null) ? vhost.ToLower (CultureInfo.InvariantCulture) : null;
			this.vport = vport;
			this.vpath = vpath;
			if (vpath == null || vpath == "" || vpath [0] != '/')
				throw new ArgumentException ("Virtual path must begin with '/': " + vpath,
								"vpath");

			this.realPath = realPath;
			this.AppHost = null;

			if (vhost != null && this.vhost.Length != 0 && this.vhost [0] == '*') {
				haveWildcard = true;
				if (this.vhost.Length > 2 && this.vhost [1] == '.')
					this.vhost = this.vhost.Substring (2);
			}
		}


		public bool TryClearHost (IApplicationHost host)
		{
			if (this.AppHost == host) {
				this.AppHost = null;
				return true;
			}

			return false;
		}

		public void UnloadHost ()
		{
			if (AppHost != null)
				AppDomain.Unload (AppHost.Domain);

			AppHost = null;
		}

		public bool Redirect (string path, out string redirect)
		{
			redirect = null;
			int plen = path.Length;
			if (plen == this.vpath.Length - 1) {
				redirect = this.vpath;
				return true;
			}

			return false;
		}

		public bool Match (string vhost, int vport, string vpath)
		{
			if (vport != -1 && this.vport != -1 && vport != this.vport)
				return false;

			if (vhost != null && this.vhost != null) {
				int length = this.vhost.Length;
				string lwrvhost = vhost.ToLower (CultureInfo.InvariantCulture);
				if (haveWildcard) {
					if (this.vhost == "*")
						return true;

					if (length > vhost.Length)
						return false;

					if (length == vhost.Length && this.vhost != lwrvhost)
						return false;

					if (vhost [vhost.Length - length - 1] != '.')
						return false;

					if (!lwrvhost.EndsWith (this.vhost))
						return false;

				} else if (this.vhost != lwrvhost) {
					return false;
				}
			}

			int local = vpath.Length;
			int vlength = this.vpath.Length;
			if (vlength > local) {
				// Check for /xxx requests to be redirected to /xxx/
				if (this.vpath [vlength - 1] != '/')
					return false;

				return (vlength - 1 == local && this.vpath.Substring (0, vlength - 1) == vpath);
			}

			return (vpath.StartsWith (this.vpath));
		}

		public void CreateHost (ApplicationServer server, IWebSource webSource)
		{
			string v = vpath;
			if (v != "/" && v.EndsWith ("/")) {
				v = v.Substring (0, v.Length - 1);
			}
			
			AppHost = ApplicationHost.CreateApplicationHost (webSource.GetApplicationHostType(), v, realPath) as IApplicationHost;
			AppHost.Server = server;
			
			// Link the host in the application domain with a request broker in the main domain
			RequestBroker = webSource.CreateRequestBroker ();
			AppHost.RequestBroker = RequestBroker;
		}
	}
	
	class HttpErrors
	{
		static byte [] error500;

		static HttpErrors ()
		{
			string s = "HTTP/1.0 500 Server error\r\n\r\n" +
				   "<html><head><title>500 Server Error</title><body><h1>Server error</h1>\r\n" +
				   "Your client sent a request that was not understood by this server.\r\n" +
				   "</body></html>\r\n";
			error500 = Encoding.ASCII.GetBytes (s);
		}

		public static byte [] NotFound (string uri)
		{
			string s = String.Format ("HTTP/1.0 404 Not Found\r\n\r\n" + 
				"<html><head><title>404 Not Found</title></head>\r\n" +
				"<body><h1>Not Found</h1>The requested URL {0} was not found on this " +
				"server.<p>\r\n</body></html>\r\n", uri);

			return Encoding.ASCII.GetBytes (s);
		}

		public static byte [] ServerError ()
		{
			return error500;
		}
	}
}

