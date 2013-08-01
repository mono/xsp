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

namespace Mono.WebServer.Fpm {
	public static class Server
	{
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

			FileInfo[] configFiles = configDirInfo.GetFiles ("*.xml");
			ChildrenManager.StartChildren (configFiles, configurationManager);

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
	}
}
