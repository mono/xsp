//
// Mono.WebServer.XSP/main.cs: Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
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
using System.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Hosting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Protocol.Tls;
using Mono.WebServer;

namespace Mono.WebServer.XSP
{
	public class Server
	{
		static RSA key;
		
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
			Console.WriteLine ("XSP server is a sample server that hosts the ASP.NET runtime in a");
			Console.WriteLine ("minimalistic HTTP server\n");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    xsp.exe [...]");
			Console.WriteLine ();
			Console.WriteLine ("    --port N: n is the tcp port to listen on.");
			Console.WriteLine ("                    Default value: 8080");
			Console.WriteLine ("                    AppSettings key name: MonoServerPort");
			Console.WriteLine ("    --random-port: listen on a randomly assigned port. The port numer");
			Console.WriteLine ("                    will be reported to the caller via a text file.");
			Console.WriteLine ();
			Console.WriteLine ("    --address addr: addr is the ip address to listen on.");
			Console.WriteLine ("                    Default value: 0.0.0.0");
			Console.WriteLine ("                    AppSettings key name: MonoServerAddress");
			Console.WriteLine ();
			Console.WriteLine ("    --https:        enable SSL for the server");
			Console.WriteLine ("                    Default value: false.");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --https-client-accept: enable SSL for the server with optional client certificates");
			Console.WriteLine ("                    Default value: false (non-ssl).");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --https-client-require: enable SSL for the server with mandatory client certificates");
			Console.WriteLine ("                    Default value: false (non-ssl).");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --cert FILENAME: path to X.509 certificate file (cer)");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --pkfile FILENAME: path to private key file (pvk)");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --p12file FILENAME: path to a PKCS#12 file containing the certificate and the private");
			Console.WriteLine ("                    AppSettings key name: ");
			Console.WriteLine ();
			Console.WriteLine ("    --pkpwd PASSWORD: password to decrypt the private key");
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
			Console.WriteLine ("    --nonstop: don't stop the server by pressing enter. Must be used");
			Console.WriteLine ("               when the server has no controlling terminal.");
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
			Master = 1 << 11,
			RandomPort = 1 << 12
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

			if ((options & Options.Port) != 0 && value == Options.RandomPort) {
				Console.WriteLine ("ERROR: --port and --random-port are mutually exclusive");
				Environment.Exit (1);
			}
		}

		static AsymmetricAlgorithm GetPrivateKey (X509Certificate certificate, string targetHost) 
		{ 
			return key; 
		}

		static NameValueCollection AppSettings {
			get {
#if NET_2_0
				return ConfigurationManager.AppSettings;
#else
				return ConfigurationSettings.AppSettings;
#endif
			}
		}

		public static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;

			Console.WriteLine ("Handling exception type {0}", ex.GetType ().Name);
			Console.WriteLine ("Message is {0}", ex.Message);
			Console.WriteLine ("IsTerminating is set to {0}", e.IsTerminating);
		}

		public static int Main (string [] args)
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler (CurrentDomain_UnhandledException);

			SecurityConfiguration security = new SecurityConfiguration ();
			bool nonstop = false;
			bool verbose = false;
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			string apps = AppSettings ["MonoApplications"];
			string appConfigDir = AppSettings ["MonoApplicationsConfigDir"];
			string appConfigFile = AppSettings ["MonoApplicationsConfigFile"];
			string rootDir = AppSettings ["MonoServerRootDir"];
			object oport;
			string ip = AppSettings ["MonoServerAddress"];
			bool master = false;
			
			if (ip == null || ip.Length == 0)
				ip = "0.0.0.0";

			oport = AppSettings ["MonoServerPort"];
			if (oport == null)
				oport = 8080;

			Options options = 0;
			int hash = 0;
			for (int i = 0; i < args.Length; i++){
				string a = args [i];
				int idx = (i + 1 < args.Length) ? i + 1 : i;
				hash ^= args [idx].GetHashCode () + i;
				
				switch (a){
				case "--https":
					CheckAndSetOptions (a, Options.Https, ref options);
					security.Enabled = true;
					break;
				case "--https-client-accept":
					CheckAndSetOptions (a, Options.Https, ref options);
					security.Enabled = true;
					security.AcceptClientCertificates = true;
					security.RequireClientCertificates = false;
					break;
				case "--https-client-require":
					CheckAndSetOptions (a, Options.Https, ref options);
					security.Enabled = true;
					security.AcceptClientCertificates = true;
					security.RequireClientCertificates = true;
					break;
				case "--p12file":
					security.Pkcs12File = args [++i];
					break;
				case "--cert":
					security.CertificateFile = args [++i];
					break;
				case "--pkfile":
					security.PvkFile = args [++i];
					break;
				case "--pkpwd":
					security.Password = args [++i];
					break;
				case "--protocols":
					security.SetProtocol (args [++i]);
					break;
				case "--port":
					CheckAndSetOptions (a, Options.Port, ref options);
					oport = args [++i];
					break;
				case "--random-port":
					CheckAndSetOptions (a, Options.RandomPort, ref options);
					oport = 0;
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
				case "--pidfile": {
					string portfile = args[++i];
					if (portfile != null && portfile.Length > 0) {
						using (StreamWriter sw = File.CreateText (portfile))
							sw.Write (Process.GetCurrentProcess ().Id);
					}
					break;
				}
					
				default:
					Console.WriteLine ("Unknown argument: {0}", a);
					ShowHelp ();
					return 1;
				}
			}

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

			if (rootDir != null && rootDir != "") {
				try {
					Environment.CurrentDirectory = rootDir;
				} catch (Exception e) {
					Console.WriteLine ("Error: {0}", e.Message);
					return 1;
				}
			}

			rootDir = Directory.GetCurrentDirectory ();
			
			WebSource webSource;
			if (security.Enabled) {
				try {
					key = security.KeyPair;
					webSource = new XSPWebSource (ipaddr, port, security.Protocol, security.ServerCertificate, 
						new PrivateKeySelectionCallback (GetPrivateKey), 
						security.AcceptClientCertificates, security.RequireClientCertificates);
				}
				catch (CryptographicException ce) {
					Console.WriteLine (ce.Message);
					return 1;
				}
			} else {
				webSource = new XSPWebSource (ipaddr, port);
			}

			ApplicationServer server = new ApplicationServer (webSource);
			server.Verbose = verbose;

			Console.WriteLine (Assembly.GetExecutingAssembly ().GetName ().Name);
			if (apps != null)
				server.AddApplicationsFromCommandLine (apps);

			if (appConfigFile != null)
				server.AddApplicationsFromConfigFile (appConfigFile);

			if (appConfigDir != null)
				server.AddApplicationsFromConfigDirectory (appConfigDir);

			if (!master && apps == null && appConfigDir == null && appConfigFile == null)
				server.AddApplicationsFromCommandLine ("/:.");

			Console.WriteLine ("Listening on address: {0}", ip);
			Console.WriteLine ("Root directory: {0}", rootDir);

			try {
				if (server.Start (!nonstop) == false)
					return 2;
				
				Console.WriteLine ("Listening on port: {0} {1}", server.Port, security);
				if (port == 0)
					Console.Error.WriteLine ("Random port: {0}", server.Port);
				
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

