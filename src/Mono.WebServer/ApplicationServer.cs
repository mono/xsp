//
// ApplicationServer.cs
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (c) Copyright 2002-2007 Novell, Inc
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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Text;
using System.Threading;
using System.IO;
using Mono.WebServer.Log;

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
		readonly WebSource webSource;
		bool started;
		bool stop;
		Socket listen_socket;

		Thread runner;

		// This is much faster than hashtable for typical cases.
		readonly List<VPathToHost> vpathToHost = new List<VPathToHost> ();

		public bool SingleApplication { get; set; }

		public IApplicationHost AppHost {
			get { return vpathToHost [0].AppHost; }
			set { vpathToHost [0].AppHost = value; }
		}

		public IRequestBroker Broker {
			get { return vpathToHost [0].RequestBroker; }
			set { vpathToHost [0].RequestBroker = value; }
		}

		public int Port {
			get {
				if (listen_socket == null || !listen_socket.IsBound)
					return -1;

				var iep = listen_socket.LocalEndPoint as IPEndPoint;
				if (iep == null)
					return -1;

				return iep.Port;
			}
		}

		public string PhysicalRoot { get; private set; }

		public bool Verbose { get; set; }

		[Obsolete ("Use the .ctor that takes a 'physicalRoot' argument instead")]
		public ApplicationServer (WebSource source) : this (source, Environment.CurrentDirectory)
		{
		}

		public ApplicationServer (WebSource source, string physicalRoot)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (String.IsNullOrEmpty (physicalRoot))
				throw new ArgumentNullException ("physicalRoot");
			
			webSource = source;
			PhysicalRoot = physicalRoot;
		}

		public void AddApplication (string vhost, int vport, string vpath, string fullPath)
		{
			char dirSepChar = Path.DirectorySeparatorChar;
			if (fullPath != null && !fullPath.EndsWith (dirSepChar.ToString ()))
				fullPath += dirSepChar;
			
			// TODO: Check for duplicates, sort, optimize, etc.
			if (Verbose && !SingleApplication) {
				Logger.Write (LogLevel.Notice, "Registering application:");
				Logger.Write(LogLevel.Notice, "    Host:          {0}", vhost ?? "any");
				Logger.Write(LogLevel.Notice, "    Port:          {0}", (vport != -1) ? vport.ToString () : "any");

				Logger.Write(LogLevel.Notice, "    Virtual path:  {0}", vpath);
				Logger.Write(LogLevel.Notice, "    Physical path: {0}", fullPath);
			}

			vpathToHost.Add (new VPathToHost (vhost, vport, vpath, fullPath));
		}

 		public void AddApplicationsFromConfigDirectory (string directoryName)
 		{
			if (Verbose && !SingleApplication) {
				Logger.Write(LogLevel.Notice, "Adding applications from *.webapp files in directory '{0}'", directoryName);
			}

			var di = new DirectoryInfo (directoryName);
			if (!di.Exists) {
				Logger.Write (LogLevel.Error, "Directory {0} does not exist.", directoryName);
				return;
			}
			
			foreach (FileInfo fi in di.GetFiles ("*.webapp"))
				AddApplicationsFromConfigFile (fi.FullName);
		}

 		public void AddApplicationsFromConfigFile (string fileName)
 		{
			if (Verbose && !SingleApplication) {
				Logger.Write(LogLevel.Notice, "Adding applications from config file '{0}'", fileName);
			}

			try {
				var doc = new XmlDocument ();
				doc.Load (fileName);

				foreach (XmlElement el in doc.SelectNodes ("//web-application")) {
					AddApplicationFromElement (el);
				}
			} catch {
				Logger.Write(LogLevel.Error, "Error loading '{0}'", fileName);
				throw;
			}
		}

		void AddApplicationFromElement (XmlNode el)
		{
			XmlNode n = el.SelectSingleNode ("enabled");
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
			if (verbose && !single_app)
				Logger.Write (LogLevel.Warning, ("Ignoring vhost {0} for {1}", n.InnerText, name);
#endif

			int vport = -1;
			n = el.SelectSingleNode ("vport");
#if !MOD_MONO_SERVER
			if (n != null)
				vport = Convert.ToInt32 (n.InnerText);
#else
			// TODO: Listen on different ports
			if (verbose && !single_app)
				Logger.Write (LogLevel.Warning, ("Ignoring vport {0} for {1}", n.InnerText, name);
#endif

			AddApplication (vhost, vport, vpath, path);
		}

 		public void AddApplicationsFromCommandLine (string applications)
 		{
 			if (applications == null)
 				throw new ArgumentNullException ("applications");
 
			if (applications.Length == 0)
				return;

			if (Verbose && !SingleApplication) {
				Logger.Write(LogLevel.Notice, "Adding applications '{0}'...", applications);
			}

 			string [] apps = applications.Split (',');

			foreach (string str in apps) {
				string [] app = str.Split (':');

				if (app.Length < 2 || app.Length > 4)
					throw new ArgumentException ("Should be something like " +
								     "[[hostname:]port:]VPath:realpath");

				int vport;
				int pos = 0;

				string vhost = app.Length >= 3 ? app[pos++] : null;

				if (app.Length >= 4) {
					// FIXME: support more than one listen port.
					vport = Convert.ToInt16 (app[pos++]);
				} else {
					vport = -1;
				}

				string vpath = app [pos++];
				string realpath = app[pos++];

				if (!vpath.EndsWith ("/"))
					vpath += "/";
 
 				string fullPath = Path.GetFullPath (realpath);
				AddApplication (vhost, vport, vpath, fullPath);
 			}
 		}

		[Obsolete]
		public bool Start (bool bgThread, Exception initialException, int backlog)
		{
			return Start (bgThread, backlog);
		}
		
		public bool Start (bool bgThread, int backlog)
		{
			if (started)
				throw new InvalidOperationException ("The server is already started.");

 			if (vpathToHost == null)
 				throw new InvalidOperationException ("SetApplications must be called first.");

			if (SingleApplication) {
				var v = vpathToHost [0];
				v.AppHost = AppHost;
				// Link the host in the application domain with a request broker in the *same* domain
				// Not needed for SingleApplication and mod_mono
				v.RequestBroker = webSource.CreateRequestBroker ();
				AppHost.RequestBroker = v.RequestBroker;
			}

			listen_socket = webSource.CreateSocket ();
			listen_socket.Listen (backlog);
			runner = new Thread (RunServer) {IsBackground = bgThread};
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
			var stopThread = new Thread (RealStop);
			stopThread.Start ();
		}

		public void ShutdownSockets ()
		{
			if (listen_socket != null) {
				try {
					listen_socket.Close ();
				} catch {
				} finally {
					listen_socket = null;
				}
			}
		}

		void RealStop ()
		{
			started = false;
			runner.Abort ();
			ShutdownSockets ();
			UnloadAll ();
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

			try {
				sock.LingerState = new LingerOption (true, 15);
#if !MOD_MONO_SERVER
				sock.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 15000); // 15s
				sock.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 15000); // 15s
#endif
			} catch {
				// Ignore exceptions here for systems that do not support these options.
			}
		}

		void RunServer ()
		{
			started = true;
			var args = new SocketAsyncEventArgs ();
			args.Completed += OnAccept;
			listen_socket.AcceptAsync (args);
			if (runner.IsBackground)
				return;

			while (true) // Just sleep until we're aborted.
				Thread.Sleep (1000000);
		}

		void OnAccept (object sender, SocketAsyncEventArgs e)
		{
			if (!started && SingleApplication) {
				// We are shutting down. Last call...
				Environment.Exit (0);
			}

			Socket accepted = e.AcceptSocket;
			e.AcceptSocket = null;

			if (e.SocketError != SocketError.Success) {
				CloseSocket(accepted);
				accepted = null;
			}

			try {
				if (started)
					listen_socket.AcceptAsync (e);
			} catch (Exception ex) {
				if (accepted != null)
					CloseSocket (accepted);

				// not much we can do. fast fail by killing the process.
				Logger.Write (LogLevel.Error, "Unable to accept socket. Exiting the process. {0}", ex);
				Environment.Exit (1);
			}

			if (accepted == null)
				return;

			SetSocketOptions (accepted);
			StartRequest (accepted, 0);
		}

		void CloseSocket(Socket socket)
		{
			if (socket == null)
				return;

			// attempt a quick RST of the connection
			try {
				socket.LingerState = new LingerOption (true, 0);
			} catch {
				// ignore
			}

			try {
				socket.Close();
			} catch {
				// ignore
			}
		}

		void SendException (Socket socket, Exception ex)
		{
			var sb = new StringBuilder ();
			string now = DateTime.Now.ToUniversalTime ().ToString ("r");

			sb.Append ("HTTP/1.0 500 Server error\r\n");
			sb.AppendFormat ("Date: {0}\r\n" +
					 "Expires: {0}\r\n" +
					 "Last-Modified: {0}\r\n", now);
			sb.AppendFormat ("Expires; {0}\r\n", now);
			sb.Append ("Cache-Control: private, must-revalidate, max-age=0\r\n");
			sb.Append ("Content-Type: text/html; charset=UTF-8\r\n");
			sb.Append ("Connection: close\r\n\r\n");
			
			sb.AppendFormat ("<html><head><title>Exception: {0}</title></head><body>" +
					 "<h1>Exception caught.</h1>" +
					 "<pre>{0}</pre>" +
					 "</body></html>", ex);

			byte[] data = Encoding.UTF8.GetBytes (sb.ToString ());
			try {
				int sent = socket.Send (data);
				if (sent != data.Length)
					throw new IOException ("Blocking send did not send entire buffer");
			} catch (Exception ex2) {
				Logger.Write(LogLevel.Error, "Failed to send exception:");
				Logger.Write(ex);
				Logger.Write(LogLevel.Error, "Exception ocurred while sending:");
				Logger.Write(ex2);
			}	
		}
		
		void StartRequest (Socket accepted, int reuses)
		{
			try {
				// The next line can throw (reusing and the client closed)
				Worker worker = webSource.CreateWorker (accepted, this);
				worker.SetReuseCount (reuses);
				if (worker.IsAsync)
					worker.Run (null);
				else
					ThreadPool.QueueUserWorkItem (worker.Run);
			} catch (Exception) {
				try {
					if (accepted != null) {
						try {
							if (accepted.Connected)
								accepted.Shutdown (SocketShutdown.Both);
						} catch {
							// ignore
						}
						
						accepted.Close ();
					}
				} catch {
					// ignore
				}
			}
		}

		public void ReuseSocket (Socket sock, int reuses)
		{
			StartRequest (sock, reuses);
		}

		public VPathToHost GetApplicationForPath (string vhost, int port, string path,
							  bool defaultToRoot)
		{
			if (SingleApplication)
				return vpathToHost [0];

			VPathToHost bestMatch = null;
			int bestMatchLength = 0;

			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				var v = vpathToHost [i];
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

			if (Verbose)
				Logger.Write(LogLevel.Error, "No application defined for: {0}:{1}{2}", vhost, port, path);

			return null;
		}

		public VPathToHost GetSingleApp ()
		{
			if (vpathToHost.Count == 1)
				return vpathToHost [0];
			return null;
		}

		public void DestroyHost (IApplicationHost host)
		{
			// Called when the host appdomain is being unloaded
			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				var v = vpathToHost [i];
				if (v.TryClearHost (host))
					break;
			}
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}

