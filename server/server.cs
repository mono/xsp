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
		}
		
		public static int Main (string [] args)
		{
			object oport;
			
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			string virtualDir = ConfigurationSettings.AppSettings ["MonoServerVirtualDir"];
			string rootDir = ConfigurationSettings.AppSettings ["MonoServerRootDir"];
			string ip = ConfigurationSettings.AppSettings ["MonoServerAddress"];
			
			if (ip == "" || ip == null)
				ip = "0.0.0.0";

			oport = ConfigurationSettings.AppSettings ["MonoServerPort"];
			if (oport == null)
				oport = 8080;

			for (int i = 0; i < args.Length; i++){
				string a = args [i];
				
				switch (a){
				case "--port":
					oport = args [++i];
					break;
				case "--root":
					rootDir = args [++i];
					break;
				case "--virtual":
					virtualDir = args [++i];
					break;
				case "--address":
					ip = args [++i];
					break;
				case "--help":
					ShowHelp ();
					return 0;
				}
			}
			
			ushort port;
			try {
				port = Convert.ToUInt16 (oport);
			} catch (Exception) {
				Console.WriteLine ("The value given for the listen port is not valid: " + oport);
				return 1;
			}
			
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

			host.SetListenAddress(IPAddress.Parse(ip), port);
			
			Console.WriteLine ("Listening on port: {0}", port);
			Console.WriteLine ("Listening on address: {0}", ip);
			Console.WriteLine ("Root directory: {0}", rootDir);
			Console.WriteLine ("Virtual directory: {0}", virtualDir);

			host.Start ();

			return 0;
		}
	}
}

