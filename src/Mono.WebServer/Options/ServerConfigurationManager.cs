using System;
using System.Net;

namespace Mono.WebServer.Options {
	public abstract class ServerConfigurationManager : ConfigurationManager, IHelpConfigurationManager
	{
		#region Backing fields
		protected readonly StringSetting root = new StringSetting ("root", Descriptions.Root, "MonoServerRootDir", "MONO_ROOT|MONO_FCGI_ROOT", Environment.CurrentDirectory);
		readonly StringSetting applications = new StringSetting ("applications", Descriptions.Applications, "MonoApplications", "MONO_APPLICATIONS|MONO_FCGI_APPLICATIONS");
		readonly StringSetting appConfigFile = new StringSetting ("appconfigfile", Descriptions.AppConfigFile, "MonoApplicationsConfigFile", "MONO_APPCONFIGFILE|MONO_FCGI_APPCONFIGFILE");
		readonly StringSetting appConfigDir = new StringSetting ("appconfigdir", Descriptions.AppConfigDir, "MonoApplicationsConfigDir", "MONO_APPCONFIGDIR|MONO_FCGI_APPCONFIGDIR");

		readonly UInt32Setting backlog = new UInt32Setting ("backlog", "The listen backlog.", defaultValue: 500);

		protected readonly Setting<IPAddress> address = new Setting<IPAddress> ("address", IPAddress.TryParse, Descriptions.Address, "MonoServerAddress", "MONO_ADDRESS|MONO_FCGI_ADDRESS", IPAddress.Loopback);
		#endregion

		#region Typesafe properties
		public string Root {
			get { return root; }
		}
		public string Applications {
			get { return applications; }
		}
		public string AppConfigFile {
			get { return appConfigFile; }
		}
		public string AppConfigDir {
			get { return appConfigDir; }
		}

		public uint Backlog {
			get { return backlog; }
		}

		public IPAddress Address { get { return address; } }
		#endregion

		public abstract string Name { get; }
		public abstract string Description { get; }

		protected ServerConfigurationManager ()
		{
			Add (root, applications, appConfigFile, appConfigDir,
				backlog,
				address);
		}
	}
}
