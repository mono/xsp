//
// main.cs:
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (c) 2013 Leonardo Taglialegne.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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

			StartChildren (configDirInfo, configurationManager);

			if (configurationManager.Stoppable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
				foreach (ChildInfo child in children) {
					// TODO: this is a bit brutal, be nicer
					try {
						if (!child.Process.HasExited)
							child.Process.Kill ();
					} catch (InvalidOperationException) {
						// Died between the if and the kill
					}
				}
			}
			return 0;
		}

		static void StartChildren (DirectoryInfo configDirInfo, ConfigurationManager configurationManager)
		{
			foreach (var fileInfo in configDirInfo.GetFiles ("*.xml")) {
				Logger.Write (LogLevel.Debug, "Loading {0}", fileInfo.Name);
				var childConfigurationManager = new ChildConfigurationManager ();
				childConfigurationManager.LoadXmlConfig (fileInfo.FullName);
				string user = childConfigurationManager.User;

				if (Platform.IsUnix) {
					if (String.IsNullOrEmpty (user)) {
						Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner", fileInfo.Name);
						user = UnixFileSystemInfo.GetFileSystemEntry (fileInfo.FullName).OwnerUser.UserName;
					}

					using (var identity = new WindowsIdentity (user)) {
						using (identity.Impersonate ()) {
							SpawnChild (configurationManager, fileInfo);
						}
					}
				} else {
					Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to the current one", fileInfo.Name);
					SpawnChild (configurationManager, fileInfo);
				}
			}
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
