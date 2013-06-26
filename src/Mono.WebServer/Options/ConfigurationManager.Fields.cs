using System;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		protected ConfigurationManager ()
		{
			settings = new SettingsCollection {help, version, verbose};
		}

		#region Baking fields
		readonly BoolSetting help = new BoolSetting ("help", "Shows this help message and exits.", prototype: "?|h|help");
		readonly BoolSetting version = new BoolSetting ("version", "Displays version information and exits.", "V|version");
		readonly BoolSetting verbose = new BoolSetting ("verbose", "Prints extra messages. Mainly useful for debugging.", prototype: "v|verbose");
		#endregion

		#region Typesafe properties
		public bool Help { get { return help; } }
		public bool Version { get { return version; } }
		public bool Verbose { get { return verbose; } }
		#endregion

		public void PrintHelp ()
		{
			WebServer.Version.Show ();
			Console.WriteLine (Description);
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    {0} [...]", Name);
			Console.WriteLine ();
			CreateOptionSet ().WriteOptionDescriptions (Console.Out);
		}

		protected void Add (params ISetting[] settings)
		{
			foreach (ISetting setting in settings)
				this.settings.Add (setting);
		}
	}
}
