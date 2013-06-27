using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	class ConfigurationManager : Options.ConfigurationManager
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

		protected override string Name {
			get { return "mono-fpm"; }
		}

		protected override string Description
		{
			get { return "mono-fpm is a FastCgi process manager."; }
		}
	}
}
