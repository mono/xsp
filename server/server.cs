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
using System.Web.Hosting;
using System.Net;

namespace Mono.ASPNET
{
	public class Server
	{
		public static void ShowHelp ()
		{
#if MODMONO_SERVER
			Console.WriteLine ("mod-mono-server.exe is a ASP.NET server used from mod_mono_unix.");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    mod-mono-server.exe [--filename file] [--root rootdir] [--virtual virtualdir]");
			Console.WriteLine ();
			Console.WriteLine ("    --filename file: the unix socket file name.");
			Console.WriteLine ("                    Default value: /tmp/mod_mono_server");
			Console.WriteLine ("                    AppSettings key name: FileNameUnix");
			Console.WriteLine ("    --root rootdir: rootdir is the root directory for the application.");
			Console.WriteLine ("                    Default value: current directory");
			Console.WriteLine ("                    AppSettings key name: MonoRootDir");
			Console.WriteLine ("    --virtual virtualdir: virtualdir is the virtual directory mapped to rootdir.");
			Console.WriteLine ("                    Default value: /");
			Console.WriteLine ("                    AppSettings key name: MonoVirtualDir");
			Console.WriteLine ();
#else
			Console.WriteLine ("XSP server is a sample server that hosts the ASP.NET runtime in a");
			Console.WriteLine ("minimalistic HTTP server\n");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    xsp.exe [--port N] [--root rootdir] [--virtual virtualdir]");
			Console.WriteLine ();
			Console.WriteLine ("    --port N: n is the tcp port to listen on.");
			Console.WriteLine ("                    Default value: 8080");
			Console.WriteLine ("                    AppSettings key name: MonoServerPort");
			Console.WriteLine ("    --address addr: addr is the ip address to listen on.");
			Console.WriteLine ("                    Default value: 0.0.0.0");
			Console.WriteLine ("                    AppSettings key name: MonoServerAddress");
			Console.WriteLine ("    --root rootdir: rootdir is the root directory for the application.");
			Console.WriteLine ("                    Default value: current directory");
			Console.WriteLine ("                    AppSettings key name: MonoRootDir");
			Console.WriteLine ("    --virtual virtualdir: virtualdir is the virtual directory mapped to rootdir.");
			Console.WriteLine ("                    Default value: /");
			Console.WriteLine ("                    AppSettings key name: MonoVirtualDir");
			Console.WriteLine ();
#endif
		}
		
		public static int Main (string [] args)
		{
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			string virtualDir = ConfigurationSettings.AppSettings ["MonoServerVirtualDir"];
			string rootDir = ConfigurationSettings.AppSettings ["MonoServerRootDir"];
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
				case "--virtual":
					virtualDir = args [++i];
					break;
				case "--help":
					ShowHelp ();
					return 0;
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
			
			if (virtualDir == null || virtualDir == "")
				virtualDir = "/";
			else if (virtualDir [0] != '/')
				virtualDir = "/" + virtualDir;

			if (rootDir != null && rootDir != "")
				Environment.CurrentDirectory = rootDir;

			rootDir = Directory.GetCurrentDirectory ();
			
			Type type = typeof (XSPApplicationHost);
			XSPApplicationHost host;
			host =  (XSPApplicationHost) ApplicationHost.CreateApplicationHost (type, virtualDir, rootDir);

#if MODMONO_SERVER
			host.SetListenFile (filename);
			Console.WriteLine ("Listening on: {0}", filename);
#else
			host.SetListenAddress (IPAddress.Parse (ip), port);
			Console.WriteLine ("Listening on port: {0}", port);
			Console.WriteLine ("Listening on address: {0}", ip);
#endif
			
			Console.WriteLine ("Root directory: {0}", rootDir);
			Console.WriteLine ("Virtual directory: {0}", virtualDir);

			host.Start ();

			return 0;
		}
	}
}

