using System.IO;
using Mono.WebServer.Options;

namespace Mono.WebServer.Apache {
	class ConfigurationManager : Options.ConfigurationManager
	{
		public ConfigurationManager (bool quietDefault, string rootDefault)
		{
			Add (nonstop, quiet, noHidden, terminate, master,
				port, backlog, minThreads,
				filename, pidFile);
			quiet.MaybeUpdate (SettingSource.Default, quietDefault);
			if (rootDefault != null)
				root.MaybeUpdate (SettingSource.Default, rootDefault);
		}

		#region Backing fields
		readonly BoolSetting nonstop = new BoolSetting ("nonstop", "Don't stop the server by pressing enter. Must be used when the server has no controlling terminal.");
		readonly BoolSetting quiet = new BoolSetting ("quiet", "Disable the initial start up information.");
		readonly BoolSetting noHidden = new BoolSetting ("no-hidden", "Allow access to hidden files (see 'man xsp' for details).");
		readonly BoolSetting terminate = new BoolSetting ("terminate", Descriptions.Terminate);
		readonly BoolSetting master = new BoolSetting ("master", Descriptions.Master);

		readonly NullableUInt16Setting port = new NullableUInt16Setting ("port", Descriptions.Port, "MonoServerPort", "MONO_FCGI_PORT");
		readonly Int32Setting backlog = new Int32Setting ("backlog", "The listen backlog.", defaultValue: 500);
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
		public int Backlog {
			get { return backlog; }
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

		protected override string Name {
			get { return "mod-mono-server.exe"; }
		}

		protected override string Description {
			get { return "mod-mono-server.exe is a ASP.NET server used from mod_mono."; }
		}
	}
}