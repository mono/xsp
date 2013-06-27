using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using Mono.Unix;
using Mono.WebServer.Log;
using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	public static class Server {
		static List<ChildInfo> children = new List<ChildInfo> ();

		public static int Main (string [] args)
		{
			var configurationManager = new ConfigurationManager ();
			if (!configurationManager.LoadCommandLineArgs (args))
				return 1;

			// Show the help and exit.
			if (configurationManager.Help) {
				configurationManager.PrintHelp ();
#if DEBUG
				Console.WriteLine ("Press any key...");
				Console.ReadKey ();
#endif
				return 0;
			}

			// Show the version and exit.
			if (configurationManager.Version) {
				Version.Show ();
				return 0;
			}

			// Enable console logging during Main ().
			Logger.WriteToConsole = true;

			if (!configurationManager.LoadConfigFile ())
				return 1;

#if DEBUG
			// Log everything while debugging
			Logger.Level = LogLevel.All;
#endif

			configurationManager.SetupLogger ();

			Logger.Write (LogLevel.Debug,
				Assembly.GetExecutingAssembly ().GetName ().Name);
			
			string configDir = configurationManager.ConfigDir;
			if (String.IsNullOrEmpty (configDir)) {
				Logger.Write (LogLevel.Error, "You MUST provide a configuration directory with the --config-dir parameter");
				return 1;
			}

			var configDirInfo = new DirectoryInfo (configDir);
			if (!configDirInfo.Exists) {
				Logger.Write (LogLevel.Error, "The configuration directory \"{0}\" does not exist!", configDir);
				return 1;
			}

			foreach (var fileInfo in configDirInfo.EnumerateFiles("*.xml")) {
				
				var childConfigurationManager = new ChildConfigurationManager();
				childConfigurationManager.LoadXmlConfig (fileInfo.FullName);
				string user = childConfigurationManager.User;
				if (String.IsNullOrEmpty (user)) {
					Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner");
					user = UnixFileSystemInfo.GetFileSystemEntry (fileInfo.FullName).OwnerUser.UserName;
				}
				using (var identity = new WindowsIdentity(user))
				using (identity.Impersonate ()) {
					var info = new ChildInfo {
						Process = new Process {
							StartInfo = new ProcessStartInfo {
								FileName = configurationManager.FastCgiCommand,
								Arguments = String.Format ("--config-file {0}", fileInfo.FullName)
							}
						}
					};
					info.Process.Start ();
					children.Add (info);
				}
			}

			/*string root_dir;
			if (!GetRootDirectory (configurationManager, out root_dir))
				return 1;

			if (!LoadApplicationsConfig (configurationManager))
				return 1;

			var stoppable = configurationManager.Stoppable;
			server.Start (stoppable);

			if (stoppable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
				server.Stop ();
			}
			*/
			return 0;
		}
	}
}
