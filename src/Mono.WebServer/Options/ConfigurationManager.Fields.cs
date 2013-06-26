using System;
using System.Net;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		protected SettingsCollection Settings { get; private set; }

		protected ConfigurationManager ()
		{
			Settings = new SettingsCollection { help, version, verbose,
				root, applications, appConfigFile, appConfigDir,
				address};
		}

		#region Backing fields
		readonly BoolSetting help = new BoolSetting ("help","Shows this help message and exits.", prototype: "?|h|help");
		readonly BoolSetting version = new BoolSetting ("version", "Displays version information and exits.");
		readonly BoolSetting verbose = new BoolSetting ("verbose", "Prints extra messages. Mainly useful for debugging.", prototype: "v|verbose");

		protected readonly StringSetting root = new StringSetting ("root", Descriptions.Root, "MonoServerRootDir", "MONO_FCGI_ROOT", Environment.CurrentDirectory);
		readonly StringSetting applications = new StringSetting ("applications", Descriptions.Applications, "MonoApplications", "MONO_FCGI_APPLICATIONS");
		readonly StringSetting appConfigFile = new StringSetting ("appconfigfile", Descriptions.AppConfigFile, "MonoApplicationsConfigFile", "MONO_FCGI_APPCONFIGFILE");
		readonly StringSetting appConfigDir = new StringSetting ("appconfigdir", Descriptions.AppConfigDir, "MonoApplicationsConfigDir", "MONO_FCGI_APPCONFIGDIR");

		protected readonly Setting<IPAddress> address = new Setting<IPAddress> ("address", IPAddress.TryParse, Descriptions.Address, "MonoServerAddress", "MONO_FCGI_ADDRESS", IPAddress.Loopback);
		#endregion

		#region Typesafe properties
		public bool Help { get { return help; } }
		public bool Version { get { return version; } }
		public bool Verbose { get { return verbose; } }

		public string Root { get { return root; } }
		public string Applications { get { return applications; } }
		public string AppConfigFile { get { return appConfigFile; } }
		public string AppConfigDir { get { return appConfigDir; } }

		public IPAddress Address { get { return address; } }
		#endregion

		public void PrintHelp ()
		{
			WebServer.Version.Show ();
			Console.WriteLine (Description);
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    {0} [...]", Name);
			Console.WriteLine();
			CreateOptionSet ().WriteOptionDescriptions (Console.Out);
		}
	}
}
