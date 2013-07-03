using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	class ConfigurationManager : Options.ConfigurationManager, IHelpConfigurationManager
	{
		#region Backing fields
		readonly BoolSetting stoppable = new BoolSetting ("stoppable", Descriptions.Stoppable);

		readonly StringSetting configDir = new StringSetting ("config-dir", "Directory containing the configuration files.");
		readonly StringSetting fastCgiCommand = new StringSetting ("fastcgi-command", "Name (if in PATH) or full path of the fastcgi executable", defaultValue: "mono-fastcgi");
		#endregion

		#region Typesafe properties
		public bool Stoppable {
			get { return stoppable; }
		}

		public string ConfigDir {
			get { return configDir; }
		}
		public string FastCgiCommand {
			get { return fastCgiCommand; }
		}
		#endregion

		public string Name {
			get { return "mono-fpm"; }
		}

		public string Description {
			get { return "mono-fpm is a FastCgi process manager."; }
		}

		public ConfigurationManager ()
		{
			Add (stoppable,
				configDir, fastCgiCommand);
		}
	}
}
