//
// ConfigurationManager.cs
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (c) 2013 Leonardo Taglialegne.
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

using System.Net;
using Mono.WebServer.Options.Settings;
using TP = Mono.Security.Protocol.Tls;
using Mono.WebServer.Options;

namespace Mono.WebServer.XSP {
	class ConfigurationManager : ServerConfigurationManager
	{
		#region Backing fields
		readonly BoolSetting nonstop = new BoolSetting ("nonstop", "Don't stop the server by pressing enter. Must be used when the server has no controlling terminal.");
		readonly BoolSetting quiet = new BoolSetting ("quiet", "Disable the initial start up information.");
		readonly BoolSetting randomPort = new BoolSetting ("random-port", "Listen on a randomly assigned port. The port numer will be reported to the caller via a text file.");
		readonly BoolSetting https = new BoolSetting ("https", "Enable SSL for the server.");
		readonly BoolSetting httpsClientAccept = new BoolSetting ("https-client-accept", "Enable SSL for the server with optional client certificates.");
		readonly BoolSetting httpsClientRequire = new BoolSetting ("https-client-require", "Enable SSL for the server with mandatory client certificates.");
		readonly BoolSetting noHidden = new BoolSetting ("no-hidden", "Allow access to hidden files (see 'man xsp' for details).");

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

		public override string ProgramName {
			get { return "xsp"; }
		}

		public override string Description {
			get {
				return "XSP server is a sample server that hosts the ASP.NET runtime in a minimalistic HTTP server";
			}
		}

		public ConfigurationManager (string name, bool quietDefault) : base (name)
		{
			Add (nonstop, quiet, randomPort, https, httpsClientAccept, httpsClientRequire, noHidden,
			     minThreads, port,
			     p12File, cert, pkFile, pkPwd, pidFile,
			     protocols);
			address.MaybeUpdate (SettingSource.Default, IPAddress.Any);
			quiet.MaybeUpdate (SettingSource.Default, quietDefault);
		}
	}
}
