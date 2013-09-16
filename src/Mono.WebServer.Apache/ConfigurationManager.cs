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

using System.IO;
using Mono.WebServer.Options;
using Mono.WebServer.Options.Settings;

namespace Mono.WebServer.Apache {
	class ConfigurationManager : ServerConfigurationManager
	{
		#region Backing fields
		readonly BoolSetting nonstop = new BoolSetting ("nonstop", "Don't stop the server by pressing enter. Must be used when the server has no controlling terminal.");
		readonly BoolSetting quiet = new BoolSetting ("quiet", "Disable the initial start up information.");
		readonly BoolSetting noHidden = new BoolSetting ("no-hidden", "Allow access to hidden files (see 'man xsp' for details).");
		readonly BoolSetting terminate = new BoolSetting ("terminate", Descriptions.Terminate);
		readonly BoolSetting master = new BoolSetting ("master", Descriptions.Master);

		readonly NullableUInt16Setting port = new NullableUInt16Setting ("port", Descriptions.Port, "MonoServerPort", "MONO_FCGI_PORT");
		readonly NullableInt32Setting minThreads = new NullableInt32Setting ("minThreads", "The minimum number of threads the thread pool creates on startup. Increase this value to handle a sudden inflow of connections.");

		readonly StringSetting filename = new StringSetting ("filename", "A unix socket filename to listen on.", "MonoUnixSocket", "MONO_UNIX_SOCKET", Path.Combine (Path.GetTempPath (), "mod_mono_server"));
		readonly StringSetting pidFile = new StringSetting ("pidfile", "Write the process PID to the specified file.");
		#endregion

		#region Typesafe properties
		public bool NonStop {
			get { return nonstop; }
		}
		public bool Quiet {
			get { return quiet; }
		}
		public bool NoHidden {
			get { return noHidden; }
		}
		public bool Terminate {
			get { return terminate; }
		}
		public bool Master {
			get { return master; }
		}

		public ushort? Port {
			get { return port; }
		}
		public int? MinThreads {
			get { return minThreads; }
		}

		public string Filename {
			get { return filename; }
		}
		public string PidFile {
			get { return pidFile; }
		}
		#endregion

		public override string ProgramName {
			get { return "mod-mono-server.exe"; }
		}

		public override string Description {
			get { return "mod-mono-server.exe is a ASP.NET server used from mod_mono."; }
		}

		public ConfigurationManager (string name, bool quietDefault, string rootDefault) : base(name)
		{
			Add (nonstop, quiet, noHidden, terminate, master,
			     port, minThreads,
			     filename, pidFile);
			quiet.MaybeUpdate (SettingSource.Default, quietDefault);
			if (rootDefault != null)
				root.MaybeUpdate (SettingSource.Default, rootDefault);
		}
	}
}