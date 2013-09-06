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
using System.IO;
using System.Reflection;
using Mono.WebServer.Log;
using Mono.WebServer.Options;
using System.Threading;
using Mono.Unix;
using System.Linq;
using System.Collections.Generic;

namespace Mono.WebServer.Fpm {
	public static class Server
	{
		public static int Main (string [] args)
		{
			var configurationManager = new ConfigurationManager ("mono-fpm");
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
			string webDir = configurationManager.WebDir;
			if (String.IsNullOrEmpty (configDir) && (!Platform.IsUnix || String.IsNullOrEmpty (webDir))) {
				if(Platform.IsUnix)
					Logger.Write (LogLevel.Error, "You MUST provide a configuration directory with the --config-dir parameter or web directories with the --web-dir parameter.");
				else
					Logger.Write (LogLevel.Error, "You MUST provide a configuration directory with the --config-dir parameter.");
				return 1;
			}

			if (!String.IsNullOrEmpty (configDir)) {
				var configDirInfo = new DirectoryInfo (configDir);
				if (!configDirInfo.Exists) {
					Logger.Write (LogLevel.Error, "The configuration directory \"{0}\" does not exist!", configDir);
				} else {
					Logger.Write (LogLevel.Debug, "Configuration directory {0} exists, loading configuration files", configDir);

					FileInfo[] configFiles = configDirInfo.GetFiles ("*.xml");
					ChildrenManager.StartChildren (configFiles, configurationManager);
				}
			}

			if (Platform.IsUnix && !String.IsNullOrEmpty (webDir)) {
				var webDirInfo = new UnixDirectoryInfo (Path.GetFullPath (webDir));
				if (!webDirInfo.Exists) {
					Logger.Write (LogLevel.Error, "The web directory \"{0}\" does not exist!", webDir);
				} else {
					Logger.Write (LogLevel.Debug, "Web directory {0} exists, starting children", webDir);

					IEnumerable<UnixDirectoryInfo> webDirs =
						from entry in webDirInfo.GetFileSystemEntries ()
						let dir = entry as UnixDirectoryInfo
						where dir != null
						select dir;

					if (configurationManager.HttpdGroup == null) {
						Logger.Write (LogLevel.Error, "Couldn't autodetect the httpd group, you must specify it explicitly with --httpd-group");
						return 1;
					}
					if (!CheckGroupExists (configurationManager.FpmGroup) || !CheckGroupExists (configurationManager.HttpdGroup) || !CheckUserExists (configurationManager.FpmUser))
						return 1;
					ChildrenManager.StartAutomaticChildren (webDirs, configurationManager);
				}
			}

			Platform.SetIdentity (configurationManager.FpmUser, configurationManager.FpmGroup);

			if (!configurationManager.Stoppable) {
				var sleep = new ManualResetEvent (false);
				sleep.WaitOne (); // Do androids dream of electric sheep?
			}

			Console.WriteLine ("Hit Return to stop the server.");
			Console.ReadLine ();

			ChildrenManager.TermChildren();
			ChildrenManager.KillChildren();
			return 0;
		}

		static bool CheckUserExists (string user)
		{
			try {
				new UnixUserInfo (user);
				return true;
			}
			catch (ArgumentException) {
				Logger.Write (LogLevel.Error, "User {0} doesn't exist, but it's needed for automatic mode", user);
				return false;
			}
		}

		static bool CheckGroupExists (string group)
		{
			try {
				new UnixGroupInfo (group);
				return true;
			}
			catch (ArgumentException) {
				Logger.Write (LogLevel.Error, "Group {0} doesn't exist, but it's needed for automatic mode", group);
				return false;
			}
		}
	}
}
