//
// Mono.WebServer.Apache/main.cs: Mod_mono Backend for XSP.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2009 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mono.WebServer.Log;
using Mono.WebServer.Options;

namespace Mono.WebServer.Apache {
	public class Server : MarshalByRefObject {
		public static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			var ex = (Exception)e.ExceptionObject;

			Logger.Write (LogLevel.Error, "Handling exception type {0}", ex.GetType ().Name);
			Logger.Write (LogLevel.Error, "Message is {0}", ex.Message);
			Logger.Write (LogLevel.Error, "IsTerminating is set to {0}", e.IsTerminating);
			if (e.IsTerminating)
				Logger.Write (ex);
		}


		public static int Main (string [] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			try {
				var svr = new Server ();
				return svr.RealMain (args, true, null, false);
			} catch (ThreadAbortException) {
				// Single-app mode and ASP.NET appdomain unloaded
				Thread.ResetAbort ();
			}
			return 1;
		}

		//
		// Parameters:
		//
		//   args - original args passed to the program
		//   root - true means caller is in the root domain
		//   ext_apphost - used when single app mode is used, in a recursive call to
		//        RealMain from the single app domain
		//   quiet - don't show messages. Used to avoid double printing of the banner
		//
		public int RealMain (string [] args, bool root, IApplicationHost ext_apphost, bool v_quiet)
		{
			var configurationManager = new ConfigurationManager ("mod_mono", v_quiet,
				ext_apphost == null ? null : ext_apphost.Path);

			if (!configurationManager.LoadCommandLineArgs (args))
				return 1;

			// Show the help and exit.
			if (configurationManager.Help) {
				configurationManager.PrintHelp ();
#if DEBUG
				Console.WriteLine("Press any key...");
				Console.ReadKey ();
#endif
				return 0;
			}
			
			// Show the version and exit.
			if (configurationManager.Version) {
				Version.Show ();
				return 0;
			}

			var hash = GetHash (args);
			if (hash == -1) {
				Logger.Write(LogLevel.Error, "Couldn't calculate hash - should have left earlier - something is really wrong");
				return 1;
			}
			if (hash == -2) {
				Logger.Write(LogLevel.Error, "Couldn't calculate hash - unrecognized parameter");
				return 1;
			}

			if (!configurationManager.LoadConfigFile ())
				return 1;

			configurationManager.SetupLogger ();

			ushort port = configurationManager.Port ?? 0;
			bool useTCP = port != 0;
			string lockfile = useTCP ? Path.Combine (Path.GetTempPath (), "mod_mono_TCP_") : configurationManager.Filename;
			lockfile = String.Format ("{0}_{1}", lockfile, hash);

			ModMonoWebSource webSource = useTCP
				? new ModMonoTCPWebSource (configurationManager.Address, port, lockfile)
				: new ModMonoWebSource (configurationManager.Filename, lockfile);

			if(configurationManager.Terminate) {
				if (configurationManager.Verbose)
					Logger.Write (LogLevel.Notice, "Shutting down running mod-mono-server...");

				bool res = webSource.GracefulShutdown ();
				if (configurationManager.Verbose)
					if (res)
						Logger.Write (LogLevel.Notice, "Done");
					else
						Logger.Write (LogLevel.Error, "Failed.");

				return res ? 0 : 1;
			}

			var server = new ApplicationServer (webSource, configurationManager.Root) {
				Verbose = configurationManager.Verbose,
				SingleApplication = !root
			};

#if DEBUG
			Console.WriteLine (Assembly.GetExecutingAssembly ().GetName ().Name);
#endif
			if (configurationManager.Applications != null)
				server.AddApplicationsFromCommandLine (configurationManager.Applications);

			if (configurationManager.AppConfigFile != null)
				server.AddApplicationsFromConfigFile (configurationManager.AppConfigFile);

			if (configurationManager.AppConfigDir != null)
				server.AddApplicationsFromConfigDirectory (configurationManager.AppConfigDir);

			if (!configurationManager.Master && configurationManager.Applications == null
				&& configurationManager.AppConfigDir == null && configurationManager.AppConfigFile == null)
				server.AddApplicationsFromCommandLine ("/:."); // TODO: do we really want this?

			VPathToHost vh = server.GetSingleApp ();
			if (root && vh != null) {
				// Redo in new domain
				vh.CreateHost (server, webSource);
				var svr = (Server)vh.AppHost.Domain.CreateInstanceAndUnwrap (GetType ().Assembly.GetName ().ToString (), GetType ().FullName);
				webSource.Dispose ();
				return svr.RealMain (args, false, vh.AppHost, configurationManager.Quiet);
			}
			if (ext_apphost != null) {
				ext_apphost.Server = server;
				server.AppHost = ext_apphost;
			}
			if (!configurationManager.Quiet) {
				if (!useTCP)
					Logger.Write (LogLevel.Notice, "Listening on: {0}", configurationManager.Filename);
				else {
					Logger.Write (LogLevel.Notice, "Listening on port: {0}", port);
					Logger.Write (LogLevel.Notice, "Listening on address: {0}", configurationManager.Address);
				}
				Logger.Write (LogLevel.Notice, "Root directory: {0}", configurationManager.Root);
			}

			try {
				if (!server.Start (!configurationManager.NonStop, (int)configurationManager.Backlog))
					return 2;

				if (!configurationManager.NonStop) {
					Logger.Write (LogLevel.Notice, "Hit Return to stop the server.");
					while (true) {
						try {
							Console.ReadLine ();
							break;
						} catch (IOException) {
							// This might happen on appdomain unload
							// until the previous threads are terminated.
							Thread.Sleep (500);
						}
					}
					server.Stop ();
				}
			} catch (Exception e) {
				if (!(e is ThreadAbortException))
					Logger.Write (e);
				else
					server.ShutdownSockets ();
				return 1;
			}

			return 0;
		}

		static int GetHash (IEnumerable<string> args)
		{
			int hash = args.Aggregate (23, (current, arg) => current * 37 + arg.GetHashCode ());

			if (hash < 0)
				return -hash;
			return hash;
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}
