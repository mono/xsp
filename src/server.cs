//
// server.cs: Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Hosting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#if !MODMONO_SERVER
using Mono.Security.Authenticode;
using Mono.Security.Protocol.Tls;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;
#endif
using Mono.WebServer;

namespace Mono.XSP
{
	public class Server
	{
#if !MODMONO_SERVER
		static PrivateKey key;
#endif
		
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
#if MODMONO_SERVER
			Console.WriteLine ("mod-mono-server.exe is a ASP.NET server used from mod_mono.");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    mod-mono-server.exe [...]");
			Console.WriteLine ();
			Console.WriteLine ("    The arguments --filename and --port are mutually exlusive.");
			Console.WriteLine ("    --filename file: a unix socket filename to listen on.");
			Console.WriteLine ("                    Default value: /tmp/mod_mono_server");
			Console.WriteLine ("                    AppSettings key name: MonoUnixSocket");
			Console.WriteLine ();
#else
			Console.WriteLine ("XSP server is a sample server that hosts the ASP.NET runtime in a");
			Console.WriteLine ("minimalistic HTTP server\n");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    xsp.exe [...]");
			Console.WriteLine ();
#endif
			Console.WriteLine ("    --port N: n is the tcp port to listen on.");
#if MODMONO_SERVER
			Console.WriteLine ("                    Default value: none");
#else
			Console.WriteLine ("                    Default value: 8080");
#endif
			Console.WriteLine ("                    AppSettings key name: MonoServerPort");
			Console.WriteLine ();
			Console.WriteLine ("    --address addr: addr is the ip address to listen on.");
#if MODMONO_SERVER
			Console.WriteLine ("                    Default value: 127.0.0.1");
#else
			Console.WriteLine ("                    Default value: 0.0.0.0");
#endif
			Console.WriteLine ("                    AppSettings key name: MonoServerAddress");
			Console.WriteLine ();
			Console.WriteLine ("    --https:        enable SSL for the server");
			Console.WriteLine ("                    Default value: false.");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --cert FILENAME: path to X.509 certificate file");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --pkfile FILENAME: path to private key file");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --pkpwd PASSWORD: password for private key file");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --protocol:     specify which protocols are available for SSL");
			Console.WriteLine ("                    Possible values: Default, Tls, Ssl2, Ssl3");
			Console.WriteLine ("                    Default value: Default (all)");
			Console.WriteLine ("                    AppSettings key name: ");
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
#if MODMONO_SERVER
			Console.WriteLine ("    --terminate: gracefully terminates a running mod-mono-server instance.");
			Console.WriteLine ("                 All other options but --filename or --address and --port");
			Console.WriteLine ("                 are ignored if this option is provided.");
#endif
			Console.WriteLine ("    --nonstop: don't stop the server by pressing enter. Must be used");
			Console.WriteLine ("               when the server has no controlling terminal.");
			Console.WriteLine ();
			Console.WriteLine ("    --version: displays version information and exits.");
			Console.WriteLine ("    --verbose: prints extra messages. Mainly useful for debugging.");

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
		}

		static void CheckAndSetOptions (string name, Options value, ref Options options)
		{
			if ((options & value) != 0) {
				ShowHelp ();
				Console.WriteLine ();
				Console.WriteLine ("ERROR: Option '{0}' duplicated.", name);
				Environment.Exit (1);
			}

			options |= value;
			if ((options & Options.FileName) != 0 &&
			    ((options & Options.Port) != 0 || (options & Options.Address) != 0)) {
				ShowHelp ();
				Console.WriteLine ();
				Console.WriteLine ("ERROR: --port/--address and --filename are mutually exclusive");
				Environment.Exit (1);
			}
		}

#if !MODMONO_SERVER
		static AsymmetricAlgorithm GetPrivateKey (X509Certificate certificate, string targetHost) 
		{ 
			return key.RSA; 
		}
#endif

		public static int Main (string [] args)
		{
			bool secure = false;
#if !MODMONO_SERVER
			string certFilename = null, keyFilename = null, keyPassword = null, securityProtocolTypeParam = null;
			SecurityProtocolType securityProtocolType = SecurityProtocolType.Default;
			X509Certificate cert = null;
#endif
			bool nonstop = false;
			bool verbose = false;
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			string apps = ConfigurationSettings.AppSettings ["MonoApplications"];
			string appConfigDir = ConfigurationSettings.AppSettings ["MonoApplicationsConfigDir"];
			string appConfigFile = ConfigurationSettings.AppSettings ["MonoApplicationsConfigFile"];
			string rootDir = ConfigurationSettings.AppSettings ["MonoServerRootDir"];
			object oport;
			string ip = ConfigurationSettings.AppSettings ["MonoServerAddress"];
#if MODMONO_SERVER
			string filename = ConfigurationSettings.AppSettings ["MonoUnixSocket"];
#endif
			if (ip == "" || ip == null)
				ip = "0.0.0.0";

			oport = ConfigurationSettings.AppSettings ["MonoServerPort"];
			if (oport == null)
				oport = 8080;

			Options options = 0;
			int hash = 0;
			for (int i = 0; i < args.Length; i++){
				string a = args [i];
				hash ^= args [i].GetHashCode () + i;
				
				switch (a){
#if MODMONO_SERVER
				case "--filename":
					CheckAndSetOptions (a, Options.FileName, ref options);
					filename = args [++i];
					break;
				case "--terminate":
					CheckAndSetOptions (a, Options.Terminate, ref options);
					break;
#else
				case "--https":
					CheckAndSetOptions (a, Options.Https, ref options);
					secure=true;
					break;
				case "--cert":
					certFilename = args [++i];
					break;
				case "--pkfile":
					keyFilename = args [++i];
					break;
				case "--pkpwd":
					keyPassword = args [++i];
					break;
				case "--protocols":
					securityProtocolTypeParam = args [++i];
					break;
#endif
				case "--port":
					CheckAndSetOptions (a, Options.Port, ref options);
					oport = args [++i];
					break;
				case "--address":
					CheckAndSetOptions (a, Options.Address, ref options);
					ip = args [++i];
					break;
				case "--root":
					CheckAndSetOptions (a, Options.Root, ref options);
					rootDir = args [++i];
					break;
				case "--applications":
					CheckAndSetOptions (a, Options.Applications, ref options);
					apps = args [++i];
					break;
				case "--appconfigfile":
					CheckAndSetOptions (a, Options.AppConfigFile, ref options);
					appConfigFile = args [++i];
					break;
				case "--appconfigdir":
					CheckAndSetOptions (a, Options.AppConfigDir, ref options);
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
			if (hash < 0)
				hash = -hash;

			string lockfile;
			bool useTCP = ((options & Options.Port) != 0);
			if (!useTCP) {
				if (filename == null || filename == "")
					filename = "/tmp/mod_mono_server";

				if ((options & Options.Address) != 0) {
					ShowHelp ();
					Console.WriteLine ();
					Console.WriteLine ("ERROR: --address without --port");
					Environment.Exit (1);
				} lockfile = Path.Combine (Path.GetTempPath (), Path.GetFileName (filename));
				lockfile = String.Format ("{0}_{1}", lockfile, hash);
			} else {
				lockfile = Path.Combine (Path.GetTempPath (), "mod_mono_TCP_");
				lockfile = String.Format ("{0}_{1}", lockfile, hash);
			}
#endif
			IPAddress ipaddr = null;
			ushort port;
			try {
				port = Convert.ToUInt16 (oport);
			} catch (Exception) {
				Console.WriteLine ("The value given for the listen port is not valid: " + oport);
				return 1;
			}

			try {
				ipaddr = IPAddress.Parse (ip);
			} catch (Exception) {
				Console.WriteLine ("The value given for the address is not valid: " + ip);
				return 1;
			}

#if !MODMONO_SERVER
			if (secure)
			{
				if (certFilename==null) {
					Console.WriteLine ("A server X.509 certificate must be specified for a https server");
					return 1;
				}

				try {
					cert = X509Certificate.CreateFromCertFile (certFilename);
				} catch (Exception) {
					Console.WriteLine ("Unable to load X.509 certicate: " + certFilename);
					return 1;
				}

				if (keyFilename == null) {
					Console.WriteLine ("A private key file must be specified for a https server");
					return 1;
				}

				try {
					if (keyPassword == null)
						key = PrivateKey.CreateFromFile (keyFilename);
					else
						key = PrivateKey.CreateFromFile (keyFilename, keyPassword);
				} catch (CryptographicException) {
					Console.WriteLine ("Invalid private key password or private key file is corrupt");
					return 1;
				} catch (Exception ex) {
					Console.WriteLine ("Unable to load private key: " + keyFilename);
					return 1;
				}

				if (securityProtocolTypeParam != null) {
					try {
						securityProtocolType = (SecurityProtocolType) 
							Enum.Parse (typeof (SecurityProtocolType), securityProtocolTypeParam);
					} catch (Exception) {
						Console.WriteLine ("The value given for security protcol is invalid: " + securityProtocolTypeParam);
						return 1;
					}
				}
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
			
			IWebSource webSource;
#if MODMONO_SERVER
			if (useTCP) {
				webSource = new ModMonoTCPWebSource (ipaddr, port, lockfile);
			} else {
				webSource = new ModMonoWebSource (filename, lockfile);
			}

			if ((options & Options.Terminate) != 0) {
				if (verbose)
					Console.WriteLine ("Shutting down running mod-mono-server...");
				
				bool res = ((ModMonoWebSource) webSource).GracefulShutdown ();
				if (verbose)
					Console.WriteLine (res ? "Done." : "Failed");

				return (res) ? 0 : 1;
			}

			ApplicationServer server = new ApplicationServer (webSource);
#else
			if (!secure)
				webSource = new XSPWebSource (ipaddr, port);
			else
				webSource = new XSPWebSource (ipaddr, port, securityProtocolType, cert, 
					new PrivateKeySelectionCallback (GetPrivateKey), false);

			ApplicationServer server = new ApplicationServer (webSource);
#endif
			server.Verbose = verbose;

			Console.WriteLine (Assembly.GetExecutingAssembly ().GetName ().Name);
			if (apps != null)
				server.AddApplicationsFromCommandLine (apps);

			if (appConfigFile != null)
				server.AddApplicationsFromConfigFile (appConfigFile);

			if (appConfigDir != null)
				server.AddApplicationsFromConfigDirectory (appConfigDir);

			if (apps == null && appConfigDir == null && appConfigFile == null)
				server.AddApplicationsFromCommandLine ("/:.");
#if MODMONO_SERVER
			if (!useTCP) {
				Console.WriteLine ("Listening on: {0}", filename);
			} else
#endif
			{
				if (secure)
					Console.WriteLine ("Listening on port: {0} (SSL)", port);
				else
					Console.WriteLine ("Listening on port: {0}", port);
				Console.WriteLine ("Listening on address: {0}", ip);
			}
			
			Console.WriteLine ("Root directory: {0}", rootDir);

			try {
				if (server.Start (!nonstop) == false)
					return 2;

				if (!nonstop) {
					Console.WriteLine ("Hit Return to stop the server.");
					Console.ReadLine ();
					server.Stop ();
				}
			} catch (Exception e) {
				Console.WriteLine ("Error: {0}", e.Message);
				return 1;
			}

			return 0;
		}
	}
}

