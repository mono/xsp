using System.Net;
using Mono.WebServer.Options;
using TP = Mono.Security.Protocol.Tls;

namespace Mono.WebServer.XSP {
	class ConfigurationManager : Options.ConfigurationManager
	{
		public ConfigurationManager (bool quietDefault)
		{
			Add (nonstop, quiet, randomPort, https, httpsClientAccept, httpsClientRequire, noHidden,
				backlog, minThreads, port,
				p12File, cert, pkFile, pkPwd, pidFile,
				protocols);
			address.MaybeUpdate (SettingSource.Default, IPAddress.Any);
			quiet.MaybeUpdate (SettingSource.Default, quietDefault);
		}

		#region Backing fields
		readonly BoolSetting nonstop = new BoolSetting ("nonstop", "Don't stop the server by pressing enter. Must be used when the server has no controlling terminal.");
		readonly BoolSetting quiet = new BoolSetting ("quiet", "Disable the initial start up information.");
		readonly BoolSetting randomPort = new BoolSetting ("random-port", "Listen on a randomly assigned port. The port numer will be reported to the caller via a text file.");
		readonly BoolSetting https = new BoolSetting ("https", "Enable SSL for the server.");
		readonly BoolSetting httpsClientAccept = new BoolSetting ("https-client-accept", "Enable SSL for the server with optional client certificates.");
		readonly BoolSetting httpsClientRequire = new BoolSetting ("https-client-require", "Enable SSL for the server with mandatory client certificates.");
		readonly BoolSetting noHidden = new BoolSetting ("no-hidden", "Allow access to hidden files (see 'man xsp' for details).");

		readonly UInt32Setting backlog = new UInt32Setting ("backlog", "The listen backlog.", defaultValue: 500);
		readonly NullableInt32Setting minThreads = new NullableInt32Setting ("minThreads", "The minimum number of threads the thread pool creates on startup. Increase this value to handle a sudden inflow of connections.");
		readonly UInt16Setting port = new UInt16Setting ("port", Descriptions.Port, "MonoServerPort", "MONO_FCGI_PORT", 9000);

		readonly StringSetting p12File = new StringSetting ("p12file", "Path to a PKCS#12 file containing the certificate and the private.");
		readonly StringSetting cert = new StringSetting ("cert", "Path to X.509 certificate file (cer).");
		readonly StringSetting pkFile = new StringSetting ("pkfile", "Path to private key file (pvk).");
		readonly StringSetting pkPwd = new StringSetting ("pkpwd", "Password to decrypt the private key.");
		readonly StringSetting pidFile = new StringSetting ("pidfile", "Write the process PID to the specified file.");

		readonly EnumSetting<TP.SecurityProtocolType> protocols = new EnumSetting<TP.SecurityProtocolType> ("protocols", "specify which protocols are available for SSL. Possible values: Default (all), Tls, Ssl2, Ssl3", defaultValue: TP.SecurityProtocolType.Default);
		#endregion

		#region Typesafe properties
		public bool NonStop {
			get { return nonstop; }
		}
		public bool Quiet {
			get { return quiet; }
		}
		public bool RandomPort {
			get { return randomPort; }
		}
		public bool Https {
			get { return https; }
		}
		public bool HttpsClientAccept {
			get { return httpsClientAccept; }
		}
		public bool HttpsClientRequire {
			get { return httpsClientRequire; }
		}
		public bool NoHidden {
			get { return noHidden; }
		}

		public uint Backlog {
			get { return backlog; }
		}
		public int? MinThreads {
			get { return minThreads; }
		}
		public ushort Port {
			get { return port; }
		}

		public string P12File {
			get { return p12File; }
		}
		public string Cert {
			get { return cert; }
		}
		public string PkFile {
			get { return pkFile; }
		}
		public string PkPwd {
			get { return pkPwd; }
		}
		public string PidFile {
			get { return pidFile; }
		}

		public TP.SecurityProtocolType Protocols {
			get { return protocols; }
		}
		#endregion

		protected override string Name {
			get { return "xsp"; }
		}

		protected override string Description {
			get {
				return "XSP server is a sample server that hosts the ASP.NET runtime in a minimalistic HTTP server";
			}
		}
	}
}
