//
// Mono.WebServer.ModMonoWorker
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004-2007 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Net.Sockets;
using Mono.WebServer.Log;

namespace Mono.WebServer.Apache
{
	//
	// ModMonoWorker: The worker that does the initial processing of mod_mono
	// requests.
	//
	class ModMonoWorker: Worker, IDisposable
	{
		public NetworkStream Stream;
		
		readonly ApplicationServer server;
		ModMonoRequest modRequest;
		bool closed;
		int requestId = -1;
		ModMonoRequestBroker broker;
		readonly Socket client;
		
		public ModMonoWorker (Socket client, ApplicationServer server)
		{
			Stream = new NetworkStream (client, true);
			this.client = client;
			this.server = server;
		}

		static void Dispose (Action disposer, string name)
		{
			if (disposer == null)
				return;

			try {
				disposer ();
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "While disposing ModMonoWorker. {0} disposing failed with exception:", name);
				Logger.Write (ex);
			}
		}
		
		public void Dispose ()
		{
			Dispose (() => {
				if (Stream != null)
					Stream.Dispose ();
			}, "Stream");

			Dispose (() => {
				if (modRequest != null)
					modRequest.Dispose ();
			}, "modRequest");
			
			GC.SuppressFinalize (this);
		}

		public override void Run (object state)
		{
			try {
				InnerRun ();
			} catch (Exception e) {
				// FileNotFoundException might be a sign of a bad deployment
				// once we require the .exe and Mono.WebServer in bin or the GAC.

				// IOException, like EndOfStreamException, might be ok.

				if (!(e is EndOfStreamException))
					Logger.Write (e);

				try {
					// Closing is enough for mod_mono. the module will return a 50x
					if (Stream != null){
						Stream.Close ();
						Stream = null;
					}
				} catch {}
				if (!server.SingleApplication && broker != null && requestId != -1) {
					broker.UnregisterRequest (requestId);
					requestId = -1;
				}
			}
		}

		VPathToHost GetOrCreateApplication (string vhost, int port, string filepath, string virt)
		{
			string final_vdir;
			string final_pdir;
			GetPhysicalDirectory (virt, filepath, out final_vdir, out final_pdir);

			//Logger.Write (LogLevel.Error, "final_pdir: {0} final_vdir: {1}", final_pdir, final_vdir);
			VPathToHost vapp = server.GetApplicationForPath (vhost, port, virt, false);
			if (vapp == null) {
				// Don't know why this breaks mod-mono-server2.exe, but not mod-mono-server.exe
				//final_pdir = "file://" + final_pdir;
				if (final_vdir [0] != '/')
					final_vdir = "/" + final_vdir;
				server.AddApplication (vhost, port, final_vdir, final_pdir);
				vapp = server.GetApplicationForPath (vhost, port, virt, false);
			}

			return vapp;
		}

		internal static void GetPhysicalDirectory (string vfile, string pfile, out string final_vdir, out string final_pdir)
		{
			string vdir = Path.GetDirectoryName (vfile);
			string pdir = Path.GetDirectoryName (pfile);
			var vinfo = new DirectoryInfo (vdir);
			var pinfo = new DirectoryInfo (pdir);
			final_pdir = null;
			final_vdir = null;
			while (vinfo != null && pinfo != null) {
				if (IsRootDirectory (pinfo)) {
					final_pdir = pinfo.ToString ();
					final_vdir = vinfo.ToString ();
					break;
				}
				if (pinfo.Name != vinfo.Name) {
					final_vdir = vinfo.ToString ();
					break;
				}
				pinfo = pinfo.Parent;
				vinfo = vinfo.Parent;
			}
			if (final_pdir == null) {
				final_pdir = pinfo.ToString ();
			}
			if (final_vdir == null) {
				final_vdir = vinfo.ToString ();
			}
		}

		static bool IsRootDirectory (DirectoryInfo info)
		{
			if (!info.Exists)
				return false;

			return File.Exists (Path.Combine (info.FullName, "Global.asax"))
				|| File.Exists (Path.Combine (info.FullName, "global.asax"))
				|| Directory.Exists (Path.Combine (info.FullName, "bin"));
		}

		void InnerRun ()
		{
			requestId = -1;
			broker = null;
			
			var rr = new RequestReader (client);
			if (rr.ShuttingDown) {
				Close ();
				server.Stop ();
				return;
			}

			string vhost = rr.Request.GetRequestHeader ("Host");
			int port = -1;
			if (vhost != null) {
				int lead = vhost.IndexOf('[');
				int follow = lead >= 0 ? vhost.IndexOf(']') : -1;
				if (follow > lead) {
					//ipv6 address
					int colon = vhost.IndexOf("]:", StringComparison.Ordinal);
					if (colon != -1) {
						Int32.TryParse (vhost.Substring (colon + 2), out port);
						vhost = vhost.Substring(0, colon + 1);
					}
				} else {
					//ipv4 or hostname
					int colon = vhost.IndexOf (':');
					if (colon != -1) {
						Int32.TryParse (vhost.Substring (colon + 1), out port);
						vhost = vhost.Substring (0, colon);
					}
				}
				if (port <= 0 || port > 65535) {
					//No port specified, Int32.TryParse failed or invalid port number
					port = 80;
				}
			}

			string vServerName = rr.Request.GetVirtualServerName () ?? vhost;

			VPathToHost vapp;
			string vpath = rr.GetUriPath ();
			string path = rr.GetPhysicalPath ();
			if (path == null) {
				vapp = server.GetApplicationForPath (vServerName, port, vpath, false);
			} else {
				vapp = GetOrCreateApplication (vServerName, port, path, vpath);
			}

			if (vapp == null) {
				rr.NotFound ();
				Stream.Close ();
				Stream = null;
				return;
			}

			var host = (ModMonoApplicationHost) vapp.AppHost;
			if (host == null) {
				rr.NotFound ();
				Stream.Close ();
				Stream = null;
				return;
			}
			modRequest = rr.Request;
			
			if (!server.SingleApplication) {
				broker = (ModMonoRequestBroker) vapp.RequestBroker;
				broker.UnregisterRequestEvent += OnUnregisterRequest;
				requestId = broker.RegisterRequest (this);
			}
			
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
					     modRequest.GetAllHeaderValues(),
					     (requestId == -1) ? this : null);
		}

		void OnUnregisterRequest (object sender, UnregisterRequestEventArgs args)
		{
			var broker = sender as BaseRequestBroker;
			if (broker != null)
				broker.UnregisterRequestEvent -= OnUnregisterRequest;

			if (requestId == -1 || requestId != args.RequestId)
				return;
			
			requestId = -1;
		}
		
		public override int Read (byte[] buffer, int position, int size)
		{
			return modRequest.GetClientBlock (buffer, position, size);
		}
		
		public override void Write (byte[] buffer, int position, int size)
		{
			modRequest.SendResponseFromMemory (buffer, position, size);
		}

		public void Write (IntPtr ptr, int size)
		{
			modRequest.SendResponseFromMemory (ptr, size);
		}

		public override void Close ()
		{
			if (closed)
				return;

			closed = true;
			try {
				modRequest.Close ();
			} catch {} // ignore any error
			try {
				if (Stream != null) {
					Stream.Close ();
					Stream = null;
				}
			} catch {}
		}
		
		public override void Flush ()
		{
			//modRequest.Flush (); No-op
		}

		public override bool IsConnected ()
		{
			return modRequest.IsConnected ();
		}

		public void SendFile (string filename)
		{
			modRequest.SendFile (filename);
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

		public void SetOutputBuffering (bool doBuffer)
		{
			modRequest.SetOutputBuffering (doBuffer);
		}

		public bool HeadersSent {
			get { return modRequest.HeadersSent; }
		}
	}	
}
