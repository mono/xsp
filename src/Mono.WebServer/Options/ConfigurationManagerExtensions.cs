using System;
using System.Collections.Generic;
using NDesk.Options;

namespace Mono.WebServer.Options {
	public static class ConfigurationManagerExtensions {

		public static void PrintHelp<T> (this T configurationManager) where T : IHelpConfigurationManager
		{
			Version.Show ();
			Console.WriteLine (configurationManager.Description);
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    {0} [...]", configurationManager.Name);
			Console.WriteLine ();
			configurationManager.CreateOptionSet ().WriteOptionDescriptions (Console.Out);
		}

		public static bool LoadCommandLineArgs<T> (this T configurationManager, string [] cmd_args) where T : IHelpConfigurationManager
		{
			if (cmd_args == null)
				throw new ArgumentNullException ("cmd_args");

			OptionSet optionSet = configurationManager.CreateOptionSet ();

			List<string> extra;
			try {
				extra = optionSet.Parse (cmd_args);
			} catch (OptionException e) {
				Console.WriteLine ("{0}: {1}", configurationManager.Name, e.Message);
				Console.WriteLine ("Try `{0} --help' for more information.", configurationManager.Name);
				return false;
			}

			if (extra.Count > 0) {
				Console.Write ("Warning: unparsed command line arguments: ");
				foreach (string s in extra) {
					Console.Write ("{0} ", s);
				}
				Console.WriteLine ();
			}

			return true;
		}
	}
}
