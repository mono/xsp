//
// Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
//

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	public class Server
	{
		public static void ShowHelp ()
		{
#if MODMONO_SERVER
			Console.WriteLine ("mod-mono-server.exe is a ASP.NET server used from mod_mono_unix.");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    mod-mono-server.exe [--root rootdir] [--applications APPS] [--filename file]");
			Console.WriteLine ();
			Console.WriteLine ("    --filename file: the unix socket file name.");
			Console.WriteLine ("                    Default value: /tmp/mod_mono_server");
			Console.WriteLine ("                    AppSettings key name: UnixSocketFileName");
#else
			Console.WriteLine ("XSP server is a sample server that hosts the ASP.NET runtime in a");
			Console.WriteLine ("minimalistic HTTP server\n");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    xsp.exe [--root rootdir] [--applications APPS] [--virtual virtualdir]");
			Console.WriteLine ("            [--port N] [--address addr]");
			Console.WriteLine ();
			Console.WriteLine ("    --port N: n is the tcp port to listen on.");
			Console.WriteLine ("                    Default value: 8080");
			Console.WriteLine ("                    AppSettings key name: MonoServerPort");
			Console.WriteLine ();
			Console.WriteLine ("    --address addr: addr is the ip address to listen on.");
			Console.WriteLine ("                    Default value: 0.0.0.0");
			Console.WriteLine ("                    AppSettings key name: MonoServerAddress");
#endif
			Console.WriteLine ();
			Console.WriteLine ("    --root rootdir: the server changes to this directory before");
			Console.WriteLine ("                    anything else.");
			Console.WriteLine ("                    Default value: current directory.");
			Console.WriteLine ("                    AppSettings key name: MonoServerRootDir");
			Console.WriteLine ();
			Console.WriteLine ("    --applications APPS: a semicolon separated list of virtual directory and");
			Console.WriteLine ("                    real directory for all the applications we want to manage");
			Console.WriteLine ("                    with this server. The virtual and real dirs. are separated");
			Console.WriteLine ("                    by a colon.");
			Console.WriteLine ("                    Samples: /:.");
			Console.WriteLine ("                           the virtual / is mapped to the current directory.");
			Console.WriteLine ();
			Console.WriteLine ("                            /blog:../myblog");
			Console.WriteLine ("                           the virtual /blog is mapped to ../myblog");
			Console.WriteLine ();
			Console.WriteLine ("                            /:.;/blog:../myblog");
			Console.WriteLine ("                           Two applications like the above ones are handled.");
			Console.WriteLine ("                    Default value: /:.");
			Console.WriteLine ("                    AppSettings key name: MonoApplications");
			Console.WriteLine ();
			Console.WriteLine ("    --nonstop: don't stop the server by pressing enter. Must be used");
			Console.WriteLine ("               when the server has no controlling terminal.");

			Console.WriteLine ();
		}
		
		public static int Main (string [] args)
		{
			bool nonstop = false;
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			string apps = ConfigurationSettings.AppSettings ["MonoApplications"];
			string rootDir = ConfigurationSettings.AppSettings ["MonoServerRootDir"];
			if (apps == null)
				apps = "/:.";
#if MODMONO_SERVER
			string filename = ConfigurationSettings.AppSettings ["UnixSocketFileName"];
#else
			object oport;
			string ip = ConfigurationSettings.AppSettings ["MonoServerAddress"];
			
			if (ip == "" || ip == null)
				ip = "0.0.0.0";

			oport = ConfigurationSettings.AppSettings ["MonoServerPort"];
			if (oport == null)
				oport = 8080;

#endif
			for (int i = 0; i < args.Length; i++){
				string a = args [i];
				
				switch (a){
#if MODMONO_SERVER
				case "--filename":
					filename = args [++i];
					break;
#else
				case "--port":
					oport = args [++i];
					break;
				case "--address":
					ip = args [++i];
					break;
#endif
				case "--root":
					rootDir = args [++i];
					break;
				case "--applications":
					apps = args [++i];
					break;
				case "--nonstop":
					nonstop = true;
					break;
				case "--help":
					ShowHelp ();
					return 0;
				default:
					Console.WriteLine ("Unknown argument: {0}", a);
					ShowHelp ();
					return 1;
				}
			}

#if MODMONO_SERVER
			if (filename == null || filename == "")
				filename = "/tmp/mod_mono_server";
#else
			ushort port;
			try {
				port = Convert.ToUInt16 (oport);
			} catch (Exception) {
				Console.WriteLine ("The value given for the listen port is not valid: " + oport);
				return 1;
			}
#endif
			if (rootDir != null && rootDir != "")
				Environment.CurrentDirectory = rootDir;

			rootDir = Directory.GetCurrentDirectory ();
			
			Type type = typeof (XSPApplicationHost);
			XSPApplicationHost host;
			host =  (XSPApplicationHost) ApplicationHost.CreateApplicationHost (type, "/", rootDir);
			host.SetApplications (apps);

#if MODMONO_SERVER
			host.SetListenFile (filename);
			Console.WriteLine ("Listening on: {0}", filename);
#else
			host.SetListenAddress (IPAddress.Parse (ip), port);
			Console.WriteLine ("Listening on port: {0}", port);
			Console.WriteLine ("Listening on address: {0}", ip);
#endif
			
			Console.WriteLine ("Root directory: {0}", rootDir);

			ManualResetEvent evt = null;
			try {
				host.Start ();
				if (!nonstop) {
					Console.WriteLine ("Hit Return to stop the server.");
					Console.ReadLine ();
				} else {
					evt = new ManualResetEvent (false);
					evt.WaitOne ();
				}
			} catch (Exception e) {
				Console.WriteLine ("Error: {0}", e.Message);
				if (evt != null)
					evt.Set ();
				return 1;
			}
			
			if (evt != null)
				evt.Set ();

			return 0;
		}
	}
}

