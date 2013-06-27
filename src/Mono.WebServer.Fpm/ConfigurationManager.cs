using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	class ConfigurationManager : Options.ConfigurationManager, IHelpConfigurationManager
	{
		public ConfigurationManager ()
		{
			Add (configDir);
		}

		#region Backing fields
		readonly StringSetting configDir = new StringSetting ("config-dir", "Directory containing the configuration files.");
		#endregion

		#region Typesafe properties
		public string ConfigDir {
			get { return configDir; }
		}
		#endregion

		public string Name {
			get { return "mono-fpm"; }
		}

		public string Description
		{
			get { return "mono-fpm is a FastCgi process manager."; }
		}
	}
}
