//
// Mono.WebServer.Apache/main.cs: Mod_mono Backend for XSP.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2009 Novell, Inc. (http://www.novell.com)
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
using System.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Hosting;
using Mono.WebServer;

namespace Mono.WebServer.Apache
{
	public class Server : MarshalByRefObject
	{
		sealed class ApplicationSettings
		{
			public string Apps;
			public string AppConfigDir;
			public string AppConfigFile;
			public string RootDir;
			public object Oport = 8080;
			public string IP = "0.0.0.0";
			public Exception Exception;
			public bool NonStop;
			public bool Verbose;
			public bool Master;
			public string FileName;
			
			public ApplicationSettings ()
			{
				this.Exception = null;
				try {
					this.Apps = AppSettings ["MonoApplications"];
					this.AppConfigDir = AppSettings ["MonoApplicationsConfigDir"];
					this.AppConfigFile = AppSettings ["MonoApplicationsConfigFile"];
					this.RootDir = AppSettings ["MonoServerRootDir"];
					this.IP = AppSettings ["MonoServerAddress"];
					this.Oport = AppSettings ["MonoServerPort"];
					this.FileName = AppSettings ["MonoUnixSocket"];

					if (IP == null || IP.Length == 0)
						IP = "0.0.0.0";
					if (Oport == null)
						Oport = 8080;
				} catch (Exception ex) {
					Console.Error.WriteLine ("Exception caught during reading the configuration file:");
					Console.Error.WriteLine (ex);
					this.Exception = ex;
				}
			}
		}

		static void ShowVersion ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string version = assembly.GetName ().Version.ToString ();
			object [] att = assembly.GetCustomAttributes (typeof (AssemblyTitleAttribute), false);
			//string title = ((AssemblyTitleAttribute) att [0]).Title;
			att = assembly.GetCustomAttributes (typeof (AssemblyCopyrightAttribute), false);
			string copyright = ((AssemblyCopyrightAttribute) att [0]).Copyright;
			att = assembly.GetCustomAttributes (typeof (AssemblyDescriptionAttribute), false);
			string description = ((AssemblyDescriptionAttribute) att [0]).Description;
			Console.WriteLine ("{0} {1}\n(c) {2}\n{3}",
					Path.GetFileName (assembly.Location), version, copyright, description);
		}

		static void ShowHelp ()
		{
			Console.WriteLine ("mod-mono-server.exe is a ASP.NET server used from mod_mono.");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    mod-mono-server.exe [...]");
			Console.WriteLine ();
			Console.WriteLine ("    The arguments --filename and --port are mutually exlusive.");
			Console.WriteLine ("    --filename file: a unix socket filename to listen on.");
			Console.WriteLine ("                    Default value: /tmp/mod_mono_server");
			Console.WriteLine ("                    AppSettings key name: MonoUnixSocket");
			Console.WriteLine ();
			Console.WriteLine ("    --port N: n is the tcp port to listen on.");
			Console.WriteLine ("                    Default value: none");
			Console.WriteLine ("                    AppSettings key name: MonoServerPort");
			Console.WriteLine ();
			Console.WriteLine ("    --address addr: addr is the ip address to listen on.");
			Console.WriteLine ("                    Default value: 127.0.0.1");
			Console.WriteLine ("                    AppSettings key name: MonoServerAddress");
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
			Console.WriteLine ("    --terminate: gracefully terminates a running mod-mono-server instance.");
			Console.WriteLine ("                 All other options but --filename or --address and --port");
			Console.WriteLine ("                 are ignored if this option is provided.");
			Console.WriteLine ("    --master: this instance will be used to by mod_mono to create ASP.NET");
			Console.WriteLine ("              applications on demand. If this option is provided, there is no");
			Console.WriteLine ("              need to provide a list of applications to start.");
			Console.WriteLine ("    --nonstop: don't stop the server by pressing enter. Must be used");
			Console.WriteLine ("               when the server has no controlling terminal.");
			Console.WriteLine ();
			Console.WriteLine ("    --no-hidden: allow access to hidden files (see 'man xsp' for details)");
			Console.WriteLine ();
			Console.WriteLine ("    --version: displays version information and exits.");
			Console.WriteLine ("    --verbose: prints extra messages. Mainly useful for debugging.");
			Console.WriteLine ("    --pidfile file: write the process PID to the specified file.");

			Console.WriteLine ();
		}

		[Flags]
		enum Options {
			NonStop = 1,
			Verbose = 1 << 1,
			Applications = 1 << 2,
			AppConfigDir = 1 << 3,
			AppConfigFile = 1 << 4,
			Root = 1 << 5,
			FileName = 1 << 6,
			Address = 1 << 7,
			Port = 1 << 8,
			Terminate = 1 << 9,
			Https = 1 << 10,
			Master = 1 << 11
		}

		static void CheckAndSetOptions (string name, Options value, ref Options options)
		{
			if ((options & value) != 0) {
				ShowHelp ();
				Console.Error.WriteLine ();
				Console.Error.WriteLine ("ERROR: Option '{0}' duplicated.", name);
				Environment.Exit (1);
			}

			options |= value;
			if ((options & Options.FileName) != 0 &&
			    ((options & Options.Port) != 0 || (options & Options.Address) != 0)) {
				ShowHelp ();
				Console.Error.WriteLine ();
				Console.Error.WriteLine ("ERROR: --port/--address and --filename are mutually exclusive");
				Environment.Exit (1);
			}
		}

		static NameValueCollection AppSettings {
			get { return ConfigurationManager.AppSettings; }
		}

		public static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;

			Console.Error.WriteLine ("Handling exception type {0}", ex.GetType ().Name);
			Console.Error.WriteLine ("Message is {0}", ex.Message);
			Console.Error.WriteLine ("IsTerminating is set to {0}", e.IsTerminating);
			if (e.IsTerminating)
				Console.Error.WriteLine (ex);
		}

		
		public static int Main (string [] args)
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler (CurrentDomain_UnhandledException);
			bool quiet = false;
			//while (true) {
				try {
					Server svr = new Server ();
					return svr.RealMain (args, true, null, quiet);
				} catch (ThreadAbortException) {
					// Single-app mode and ASP.NET appdomain unloaded
					Thread.ResetAbort ();
					quiet = true; // hush 'RealMain'
				}
			//}
			return 1;
		}

		//
		// Parameters:
		//
		//   args - original args passed to the program
		//   root - true means caller is in the root domain
		//   ext_apphost - used when single app mode is used, in a recursive call to
		//        RealMain from the single app domain
		//   quiet - don't show messages. Used to avoid double printing of the banner
		//
		public int RealMain (string [] args, bool root, IApplicationHost ext_apphost, bool quiet)
		{
			ApplicationSettings settings = new ApplicationSettings ();
			if (ext_apphost != null)
				settings.RootDir = ext_apphost.Path;

			Options options = 0;
			int hash = 0;
			for (int i = 0; i < args.Length; i++){
				string a = args [i];
				int idx = (i + 1 < args.Length) ? i + 1 : i;
				hash ^= args [idx].GetHashCode () + i;
				
				switch (a){
				case "--filename":
					CheckAndSetOptions (a, Options.FileName, ref options);
					settings.FileName = args [++i];
					break;
				case "--terminate":
					CheckAndSetOptions (a, Options.Terminate, ref options);
					break;
				case "--master":
					CheckAndSetOptions (a, Options.Master, ref options);
					settings.Master = true;
					break;
				case "--port":
					CheckAndSetOptions (a, Options.Port, ref options);
					settings.Oport = args [++i];
					break;
				case "--address":
					CheckAndSetOptions (a, Options.Address, ref options);
					settings.IP = args [++i];
					break;
				case "--root":
					CheckAndSetOptions (a, Options.Root, ref options);
					settings.RootDir = args [++i];
					break;
				case "--applications":
					CheckAndSetOptions (a, Options.Applications, ref options);
					settings.Apps = args [++i];
					break;
				case "--appconfigfile":
					CheckAndSetOptions (a, Options.AppConfigFile, ref options);
					settings.AppConfigFile = args [++i];
					break;
				case "--appconfigdir":
					CheckAndSetOptions (a, Options.AppConfigDir, ref options);
					settings.AppConfigDir = args [++i];
					break;
				case "--nonstop":
					settings.NonStop = true;
					break;
				case "--help":
					ShowHelp ();
					return 0;
				case "--version":
					ShowVersion ();
					return 0;
				case "--verbose":
					settings.Verbose = true;
					break;
				case "--pidfile": {
					string pidfile = args[++i];
					if (pidfile != null && pidfile.Length > 0) {
						try {
							using (StreamWriter sw = File.CreateText (pidfile))
								sw.Write (Process.GetCurrentProcess ().Id);
						} catch (Exception ex) {
							Console.Error.WriteLine ("Failed to write pidfile {0}: {1}", pidfile, ex.Message);
						}
					}
					break;
				}
				case "--no-hidden":
					MonoWorkerRequest.CheckFileAccess = false;
					break;
				default:
					Console.Error.WriteLine ("Unknown argument: {0}", a);
					ShowHelp ();
					return 1;
				}
			}

			if (hash < 0)
				hash = -hash;

			string lockfile;
			bool useTCP = ((options & Options.Port) != 0);
			if (!useTCP) {
				if (settings.FileName == null || settings.FileName.Length == 0)
					settings.FileName = "/tmp/mod_mono_server";

				if ((options & Options.Address) != 0) {
					ShowHelp ();
					Console.Error.WriteLine ();
					Console.Error.WriteLine ("ERROR: --address without --port");
					Environment.Exit (1);
				} lockfile = Path.Combine (Path.GetTempPath (), Path.GetFileName (settings.FileName));
				lockfile = String.Format ("{0}_{1}", lockfile, hash);
			} else {
				lockfile = Path.Combine (Path.GetTempPath (), "mod_mono_TCP_");
				lockfile = String.Format ("{0}_{1}", lockfile, hash);
			}

			IPAddress ipaddr = null;
			ushort port;
			try {
				port = Convert.ToUInt16 (settings.Oport);
			} catch (Exception) {
				Console.Error.WriteLine ("The value given for the listen port is not valid: " + settings.Oport);
				return 1;
			}

			try {
				ipaddr = IPAddress.Parse (settings.IP);
			} catch (Exception) {
				Console.Error.WriteLine ("The value given for the address is not valid: " + settings.IP);
				return 1;
			}

			if (settings.RootDir != null && settings.RootDir.Length > 0) {
				try {
					Environment.CurrentDirectory = settings.RootDir;
				} catch (Exception e) {
					Console.Error.WriteLine ("Error: {0}", e.Message);
					return 1;
				}
			}

			settings.RootDir = Directory.GetCurrentDirectory ();
			
			WebSource webSource;
			if (useTCP) {
				webSource = new ModMonoTCPWebSource (ipaddr, port, lockfile);
			} else {
				webSource = new ModMonoWebSource (settings.FileName, lockfile);
			}

			if ((options & Options.Terminate) != 0) {
				if (settings.Verbose)
					Console.Error.WriteLine ("Shutting down running mod-mono-server...");
				
				bool res = ((ModMonoWebSource) webSource).GracefulShutdown ();
				if (settings.Verbose)
					Console.Error.WriteLine (res ? "Done." : "Failed");

				return (res) ? 0 : 1;
			}

			ApplicationServer server = new ApplicationServer (webSource, settings.RootDir);
			server.Verbose = settings.Verbose;
			server.SingleApplication = !root;

			Console.WriteLine (Assembly.GetExecutingAssembly ().GetName ().Name);
			if (settings.Apps != null)
				server.AddApplicationsFromCommandLine (settings.Apps);

			if (settings.AppConfigFile != null)
				server.AddApplicationsFromConfigFile (settings.AppConfigFile);

			if (settings.AppConfigDir != null)
				server.AddApplicationsFromConfigDirectory (settings.AppConfigDir);

			if (!settings.Master && settings.Apps == null && settings.AppConfigDir == null && settings.AppConfigFile == null)
				server.AddApplicationsFromCommandLine ("/:.");

			VPathToHost vh = server.GetSingleApp ();
			if (root && vh != null) {
				// Redo in new domain
				vh.CreateHost (server, webSource);
				Server svr = (Server) vh.AppHost.Domain.CreateInstanceAndUnwrap (GetType ().Assembly.GetName ().ToString (), GetType ().FullName);
				webSource.Dispose ();
				return svr.RealMain (args, false, vh.AppHost, quiet);
			}
			if (ext_apphost != null) {
				ext_apphost.Server = server;
				server.AppHost = ext_apphost;
			}
			if (!useTCP && !quiet) {
				Console.Error.WriteLine ("Listening on: {0}", settings.FileName);
			} else if (!quiet) {
				Console.Error.WriteLine ("Listening on port: {0}", port);
				Console.Error.WriteLine ("Listening on address: {0}", settings.IP);
			}

			if (!quiet)
				Console.Error.WriteLine ("Root directory: {0}", settings.RootDir);

			try {
				if (server.Start (!settings.NonStop) == false)
					return 2;

				if (!settings.NonStop) {
					Console.Error.WriteLine ("Hit Return to stop the server.");
					while (true) {
						try {
							Console.ReadLine ();
							break;
						} catch (IOException) {
							// This might happen on appdomain unload
							// until the previous threads are terminated.
							Thread.Sleep (500);
						}
					}
					server.Stop ();
				}
			} catch (Exception e) {
				if (!(e is ThreadAbortException))
					Console.Error.WriteLine ("Error: {0}", e.Message);
				else
					server.ShutdownSockets ();
				return 1;
			}

			return 0;
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}

