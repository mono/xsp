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
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Mono.Unix;

namespace Mono.WebServer
{
	//
	// ModMonoWorker: The worker that does the initial processing of mod_mono
	// requests.
	//
	internal class ModMonoWorker: Worker
	{
		public LingeringNetworkStream Stream;
		
		ApplicationServer server;
		ModMonoRequest modRequest;
		bool closed;
		int requestId = -1;
		ModMonoRequestBroker broker;
		Socket client;
		
		public ModMonoWorker (Socket client, ApplicationServer server)
		{
			Stream = new LingeringNetworkStream (client, true);
			Stream.EnableLingering = false;
			this.client = client;
			this.server = server;
		}
			
		public override void Run (object state)
		{
			try {
				InnerRun (state);
			} catch (Exception e) {
				// FileNotFoundException might be a sign of a bad deployment
				// once we require the .exe and Mono.WebServer in bin or the GAC.

				// IOException, like EndOfStreamException, might be ok.

				if (!(e is EndOfStreamException))
					Console.Error.WriteLine (e);

				try {
					// Closing is enough for mod_mono. the module will return a 50x
					if (Stream != null){
						Stream.Close ();
						Stream = null;
					}
				} catch {}
				if (broker != null && requestId != -1) {
					broker.UnregisterRequest (requestId);
					requestId = -1;
				}
			}
		}

		VPathToHost GetOrCreateApplication (string vhost, int port, string filepath, string virt)
		{
			VPathToHost vapp = null;
			string vdir = Path.GetDirectoryName (virt);
			string pdir = Path.GetDirectoryName (filepath);
			DirectoryInfo vinfo = new DirectoryInfo (vdir);
			DirectoryInfo pinfo = new DirectoryInfo (pdir);
			string final_pdir = null;
			string final_vdir = null;
			while (vinfo != null && pinfo != null) {
				if (final_pdir == null && CheckDirectory (pinfo)) {
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


			//Console.Error.WriteLine ("final_pdir: {0} final_vdir: {1}", final_pdir, final_vdir);
			vapp = server.GetApplicationForPath (vhost, port, virt, false);
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

		static bool CheckDirectory (DirectoryInfo info)
		{
			if (!info.Exists)
				return false;

			FileInfo [] g1 = info.GetFiles ("Global.asax");
			if (g1.Length != 0)
				return true;

			g1 = info.GetFiles ("global.asax");
			if (g1.Length != 0)
				return true;

			return (info.GetDirectories ("bin").Length != 0);
		}

		void InnerRun (object state)
		{
			requestId = -1;
			broker = null;
			
			RequestReader rr = new RequestReader (client);
			if (rr.ShuttingDown) {
				Close ();
				server.Stop ();
				return;
			}

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

			string vServerName = rr.Request.GetVirtualServerName ();
			if (vServerName == null)
				vServerName = vhost;

			VPathToHost vapp = null;
			string vpath = rr.GetUriPath ();
			string path = rr.GetPhysicalPath ();
			if (path == null) {
				vapp = server.GetApplicationForPath (vServerName, port, vpath, false);
			} else {
				vapp = GetOrCreateApplication (vServerName, port, path, vpath);
			}

			if (vapp == null) {
				rr.Decline ();
				Stream.Close ();
				Stream = null;
				return;
			}

			ModMonoApplicationHost host = (ModMonoApplicationHost) vapp.AppHost;
			if (host == null) {
				rr.Decline ();
				Stream.Close ();
				Stream = null;
				return;
			}
			modRequest = rr.Request;
			
			broker = (ModMonoRequestBroker) vapp.RequestBroker;
			broker.UnregisterRequestEvent += new BaseRequestBroker.UnregisterRequestEventHandler (OnUnregisterRequest);
			
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
		}

		void OnUnregisterRequest (object sender, UnregisterRequestEventArgs args)
		{
			BaseRequestBroker broker = sender as BaseRequestBroker;
			if (broker != null)
				broker.UnregisterRequestEvent -= new BaseRequestBroker.UnregisterRequestEventHandler (OnUnregisterRequest);

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
	}	
}
