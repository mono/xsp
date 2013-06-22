namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		protected SettingsCollection Settings { get; private set; }

		protected ConfigurationManager ()
		{
			Settings = new SettingsCollection { help, version, verbose, root, applications };
		}

		#region Backing fields
		readonly BoolSetting help = new BoolSetting ("help","Shows this help message and exits.", prototype: "?|h|help");
		readonly BoolSetting version = new BoolSetting ("version", "Displays version information and exits.");
		readonly BoolSetting verbose = new BoolSetting ("verbose", "Prints extra messages. Mainly useful for debugging.", prototype: "v|verbose");

		readonly StringSetting root = new StringSetting ("root", Descriptions.Root, "MonoServerRootDir", "MONO_FCGI_ROOT");
		readonly StringSetting applications = new StringSetting ("applications", Descriptions.Applications, "MonoApplications", "MONO_FCGI_APPLICATIONS");
		#endregion

		#region Typesafe properties
		public bool Help { get { return help; } }
		public bool Version { get { return version; } }
		public bool Verbose { get { return verbose; } }

		public string Root { get { return root; } }
		public string Applications { get { return applications; } }
		#endregion
	}
}
