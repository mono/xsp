//
// ApplicationServer.cs
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (c) Copyright 2002,2003,2004 Novell, Inc
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
using System.Net;
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

namespace Mono.WebServer
{
	// ApplicationServer runs the main server thread, which accepts client 
	// connections and forwards the requests to the correct web application.
	// ApplicationServer takes an WebSource object as parameter in the 
	// constructor. WebSource provides methods for getting some objects
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
	// 2) An Worker object is created (through the WebSource), and it is
	//    queued in the thread pool.
	// 3) When the Worker's run method is called, it registers itself in
	//    the application's request broker, and gets a request id. All this is
	//    done in the main domain.
	// 4) The Worker starts the request processing by making a cross-app domain
	//    call to the application host. It passes as parameters the request id
	//    and other information already read from the request.
	// 5) The application host executes the request. When it needs to read or
	//    write request data, it performs remote calls to the request broker,
	//    passing the request id provided by the Worker.
	// 6) When the request broker receives a call from the application host,
	//    it locates the Worker registered with the provided request id and
	//    forwards the call to it.
	
	public class ApplicationServer : MarshalByRefObject
	{
		WebSource webSource;
		bool started;
		bool stop;
		bool verbose;
		Socket listen_socket;

		Thread runner;

		// This is much faster than hashtable for typical cases.
		ArrayList vpathToHost = new ArrayList ();
		
		public ApplicationServer (WebSource source)
		{
			webSource = source;
		} 

		public bool Verbose {
			get { return verbose; }
			set { verbose = value; }
		}

		public void AddApplication (string vhost, int vport, string vpath, string fullPath)
		{
			// TODO - check for duplicates, sort, optimize, etc.
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
			if (!di.Exists) {
				Console.Error.WriteLine ("Directory {0} does not exist.", directoryName);
				return;
			}
			
			foreach (FileInfo fi in di.GetFiles ("*.webapp"))
				AddApplicationsFromConfigFile (fi.FullName);
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

			n = el.SelectSingleNode ("enabled");
			if (n != null && n.InnerText.Trim () == "false")
				return;

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

			listen_socket = webSource.CreateSocket ();
			listen_socket.Listen (500);
			listen_socket.Blocking = false;
			runner = new Thread (new ThreadStart (RunServer));
			runner.IsBackground = bgThread;
			runner.Start ();
			stop = false;
			return true;
		}

		public void Stop ()
		{
			if (!started)
				throw new InvalidOperationException ("The server is not started.");

			if (stop)
				return; // Just ignore, as we're already stopping

			stop = true;	
			webSource.Dispose ();

			// A foreground thread is required to end cleanly
			Thread stopThread = new Thread (new ThreadStart (RealStop));
			stopThread.Start ();
		}

		void RealStop ()
		{
			started = false;
			runner.Abort ();
			listen_socket.Close ();
			UnloadAll ();
			Thread.Sleep (1000);
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


		AsyncCallback accept_cb;
		void RunServer ()
		{
			started = true;
			accept_cb = new AsyncCallback (OnAccept);
			listen_socket.BeginAccept (accept_cb, null);
			if (runner.IsBackground)
				return;

			while (true) // Just sleep until we're aborted.
				Thread.Sleep (1000000);
		}

		void OnAccept (IAsyncResult ares)
		{
			Socket accepted = null;
			try {
				accepted = listen_socket.EndAccept (ares);
			} catch {
			} finally {
				if (started)
					listen_socket.BeginAccept (accept_cb, null);
			}

			if (accepted == null)
				return;
			accepted.Blocking = true;
			SetSocketOptions (accepted);
			StartRequest (accepted, 0);
		}

		void StartRequest (Socket accepted, int reuses)
		{
			Worker worker = null;
			try {
				// The next line can throw (reusing and the client closed)
				worker = webSource.CreateWorker (accepted, this);
				worker.SetReuseCount (reuses);
				if (false == worker.IsAsync)
					ThreadPool.QueueUserWorkItem (new WaitCallback (worker.Run));
				else
					worker.Run (null);
			} catch (Exception) {
				try { accepted.Close (); } catch {}
			}
		}

		public void ReuseSocket (Socket sock, int reuses)
		{
			StartRequest (sock, reuses);
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
		public string realPath;
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
				AppHost.Unload ();

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

			if (vhost != null && this.vhost != null && this.vhost != "*") {
				int length = this.vhost.Length;
				string lwrvhost = vhost.ToLower (CultureInfo.InvariantCulture);
				if (haveWildcard) {
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

		public void CreateHost (ApplicationServer server, WebSource webSource)
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
	
	public class HttpErrors
	{
		static byte [] error500;
		static byte [] badRequest;

		static HttpErrors ()
		{
			string s = "HTTP/1.0 500 Server error\r\n" +
				   "Connection: close\r\n\r\n" +
				   "<html><head><title>500 Server Error</title><body><h1>Server error</h1>\r\n" +
				   "Your client sent a request that was not understood by this server.\r\n" +
				   "</body></html>\r\n";
			error500 = Encoding.ASCII.GetBytes (s);

			string br = "HTTP/1.0 400 Bad Request\r\n" + 
				"Connection: close\r\n\r\n" +
				"<html><head><title>400 Bad Request</title></head>" +
				"<body><h1>Bad Request</h1>The request was not understood" +
				"<p></body></html>";

			badRequest = Encoding.ASCII.GetBytes (br);
		}

		public static byte [] NotFound (string uri)
		{
			string s = String.Format ("HTTP/1.0 404 Not Found\r\n" + 
				"Connection: close\r\n\r\n" +
				"<html><head><title>404 Not Found</title></head>\r\n" +
				"<body><h1>Not Found</h1>The requested URL {0} was not found on this " +
				"server.<p>\r\n</body></html>\r\n", uri);

			return Encoding.ASCII.GetBytes (s);
		}

		public static byte [] BadRequest ()
		{
			return badRequest;
		}

		public static byte [] ServerError ()
		{
			return error500;
		}
	}

	public class Paths {
		private Paths ()
		{
		}

		public static void GetPathsFromUri (string uri, out string realUri, out string pathInfo)
		{
			// There's a hidden missing feature here... :)
			realUri = uri; pathInfo = "";
			string basepath = HttpRuntime.AppDomainAppPath;
			string vpath = HttpRuntime.AppDomainAppVirtualPath;
			if (vpath [vpath.Length - 1] != '/')
				vpath += '/';

			if (vpath.Length > uri.Length)
				return;

			uri = uri.Substring (vpath.Length);
			while (uri.Length > 0 && uri [0] == '/')
				uri = uri.Substring (1);

			int dot, slash;
			int lastSlash = uri.Length;
			bool windows = (Path.DirectorySeparatorChar == '\\');

			for (dot = uri.LastIndexOf ('.'); dot > 0; dot = uri.LastIndexOf ('.', dot - 1)) {
				slash = uri.IndexOf ('/', dot);
				string partial;
				if (slash == -1)
					slash = lastSlash;

				partial = uri.Substring (0, slash);
				lastSlash = slash;
				string partial_win = null;
				if (windows)
					partial_win = partial.Replace ('/', '\\');

				string path = Path.Combine (basepath, (windows ? partial_win : partial));
				if (!File.Exists (path))
					continue;
				
				realUri = vpath + uri.Substring (0, slash);
				pathInfo = uri.Substring (slash);
				break;
			}
		}
	}
}

