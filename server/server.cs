//
// Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (c) Copyright 2004 Novell, Inc. (http://www.novell.com)
//

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	public class Server
	{
		static void ShowVersion ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string version = assembly.GetName ().Version.ToString ();
			object [] att = assembly.GetCustomAttributes (typeof (AssemblyTitleAttribute), false);
			string title = ((AssemblyTitleAttribute) att [0]).Title;
			att = assembly.GetCustomAttributes (typeof (AssemblyCopyrightAttribute), false);
			string copyright = ((AssemblyCopyrightAttribute) att [0]).Copyright;
			att = assembly.GetCustomAttributes (typeof (AssemblyDescriptionAttribute), false);
			string description = ((AssemblyDescriptionAttribute) att [0]).Description;
			Console.WriteLine ("{0} {1}\n(c) {2}\n{3}",
					Path.GetFileName (assembly.Location), version, copyright, description);
		}

		static void ShowHelp ()
		{
#if MODMONO_SERVER
			Console.WriteLine ("mod-mono-server.exe is a ASP.NET server used from mod_mono.");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    mod-mono-server.exe [--root rootdir] [--applications APPS] [--filename file]");
			Console.WriteLine ("            [--appconfigdir DIR] [--appconfigfile FILE]");
			Console.WriteLine ();
			Console.WriteLine ("    --filename file: the unix socket file name.");
			Console.WriteLine ("                    Default value: /tmp/mod_mono_server");
			Console.WriteLine ("                    AppSettings key name: MonoUnixSocket");
#else
			Console.WriteLine ("XSP server is a sample server that hosts the ASP.NET runtime in a");
			Console.WriteLine ("minimalistic HTTP server\n");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    xsp.exe [--root rootdir] [--applications APPS]");
			Console.WriteLine ("            [--appconfigdir DIR] [--appconfigfile FILE]");
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
			Console.WriteLine ("    --appconfigfile FILENAME: adds application definitions from the XML");
			Console.WriteLine ("                    configuration file. See sample configuration file that");
			Console.WriteLine ("                    comes with the server.");
			Console.WriteLine ("                    AppSettings key name: MonoApplicationsConfigFile");
			Console.WriteLine ();
			Console.WriteLine ("    --appconfigdir DIR: adds application definitions from all XML files");
			Console.WriteLine ("                    found in the specified directory DIR. Files must have");
			Console.WriteLine ("                    '.webapp' extension");
			Console.WriteLine ("                    AppSettings key name: MonoApplicationsConfigDir");
			Console.WriteLine ();
			Console.WriteLine ("    --applications APPS:");
			Console.WriteLine ("                    a comma separated list of virtual directory and");
			Console.WriteLine ("                    real directory for all the applications we want to manage");
			Console.WriteLine ("                    with this server. The virtual and real dirs. are separated");
			Console.WriteLine ("                    by a colon. Optionally you may specify virtual host name");
			Console.WriteLine ("                    and a port.");
			Console.WriteLine ();
			Console.WriteLine ("                           [[hostname:]port:]VPath:realpath,...");
			Console.WriteLine ();
			Console.WriteLine ("                    Samples: /:.");
			Console.WriteLine ("                           the virtual / is mapped to the current directory.");
			Console.WriteLine ();
			Console.WriteLine ("                            /blog:../myblog");
			Console.WriteLine ("                           the virtual /blog is mapped to ../myblog");
			Console.WriteLine ();
			Console.WriteLine ("                            myhost.someprovider.net:/blog:../myblog");
			Console.WriteLine ("                           the virtual /blog at myhost.someprovider.net is mapped to ../myblog");
			Console.WriteLine ();
			Console.WriteLine ("                            /:.,/blog:../myblog");
			Console.WriteLine ("                           Two applications like the above ones are handled.");
			Console.WriteLine ("                    Default value: /:.");
			Console.WriteLine ("                    AppSettings key name: MonoApplications");
			Console.WriteLine ();
			Console.WriteLine ("    --nonstop: don't stop the server by pressing enter. Must be used");
			Console.WriteLine ("               when the server has no controlling terminal.");
			Console.WriteLine ();
			Console.WriteLine ("    --version: displays version information and exits.");
			Console.WriteLine ("    --verbose: prints extra messages. Mainly useful for debugging.");

			Console.WriteLine ();
		}
		
		public static int Main (string [] args)
		{
			bool nonstop = false;
			bool verbose = true;
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			string apps = ConfigurationSettings.AppSettings ["MonoApplications"];
			string appConfigDir = ConfigurationSettings.AppSettings ["MonoApplicationsConfigDir"];
			string appConfigFile = ConfigurationSettings.AppSettings ["MonoApplicationsConfigFile"];
			string rootDir = ConfigurationSettings.AppSettings ["MonoServerRootDir"];
#if MODMONO_SERVER
			string filename = ConfigurationSettings.AppSettings ["MonoUnixSocket"];
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
				case "--appconfigfile":
					appConfigFile = args [++i];
					break;
				case "--appconfigdir":
					appConfigDir = args [++i];
					break;
				case "--nonstop":
					nonstop = true;
					break;
				case "--help":
					ShowHelp ();
					return 0;
				case "--version":
					ShowVersion ();
					return 0;
				case "--verbose":
					verbose = true;
					break;
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

			try {
				IPAddress.Parse (ip);
			} catch (Exception) {
				Console.WriteLine ("The value given for the address is not valid: " + ip);
				return 1;
			}
#endif
			if (rootDir != null && rootDir != "") {
				try {
					Environment.CurrentDirectory = rootDir;
				} catch (Exception e) {
					Console.WriteLine ("Error: {0}", e.Message);
					return 1;
				}
			}

			rootDir = Directory.GetCurrentDirectory ();
			
			XSPApplicationServer server =  new XSPApplicationServer ();
			if (verbose)
				server.Verbose = true;
			if (apps != null)
				server.AddApplicationsFromCommandLine(apps);
			if (appConfigFile != null)
				server.AddApplicationsFromConfigFile(appConfigFile);
			if (appConfigDir != null)
				server.AddApplicationsFromConfigDirectory(appConfigDir);
			if (apps == null && appConfigDir == null && appConfigFile == null)
				server.AddApplicationsFromCommandLine("/:.");
#if MODMONO_SERVER
			server.SetListenFile (filename);
			Console.WriteLine ("Listening on: {0}", filename);
#else
			server.SetListenAddress (IPAddress.Parse (ip), port);
			Console.WriteLine ("Listening on port: {0}", port);
			Console.WriteLine ("Listening on address: {0}", ip);
#endif
			
			Console.WriteLine ("Root directory: {0}", rootDir);

			ManualResetEvent evt = null;
			try {
				if (server.Start () == false)
					return 2;

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

				Environment.Exit (1); // Temp. workaround for errors finishing
				return 1;
			}
			
			if (evt != null)
				evt.Set ();

			Environment.Exit (0); // Temp. workaround for errors finishing
			return 0;
		}
	}
}

