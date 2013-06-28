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
	public static class Server 
	{
		static readonly List<ChildInfo> children = new List<ChildInfo> ();

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

			if (!configurationManager.LoadConfigFile ())
				return 1;

			configurationManager.SetupLogger ();

#if DEBUG
			// Log everything while debugging
			Logger.Level = LogLevel.All;
#endif

			Logger.Write (LogLevel.Debug, Assembly.GetExecutingAssembly ().GetName ().Name);

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

			Logger.Write (LogLevel.Debug, "Configuration directory exists, loading configuration files");

			foreach (var fileInfo in configDirInfo.GetFiles("*.xml")) {
				Logger.Write (LogLevel.Debug, "Loading {0}", fileInfo.Name);
				var childConfigurationManager = new ChildConfigurationManager ();
				childConfigurationManager.LoadXmlConfig (fileInfo.FullName);
				string user = childConfigurationManager.User;

				if (Platform.IsUnix) {
					if (String.IsNullOrEmpty (user)) {
						Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner", fileInfo.Name);
						user = UnixFileSystemInfo.GetFileSystemEntry (fileInfo.FullName).OwnerUser.UserName;
					}

					using (var identity = new WindowsIdentity (user))
					using (identity.Impersonate ())
						SpawnChild (configurationManager, fileInfo);
				} else {
					Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to the current one", fileInfo.Name);
					SpawnChild (configurationManager, fileInfo);
				}
			}

			/*
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

		static void SpawnChild (ConfigurationManager configurationManager, FileInfo fileInfo)
		{
			var info = new ChildInfo {
				Process = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = configurationManager.FastCgiCommand,
						Arguments = String.Format ("--config-file {0}", fileInfo.FullName),
						UseShellExecute = true
					}
				}
			};
			info.Process.Start ();
			children.Add (info);
		}
	}
}