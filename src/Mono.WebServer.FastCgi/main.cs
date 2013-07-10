//
// server.cs: Web Server that uses ASP.NET hosting
//
// Authors:
//   Brian Nickel (brian.nickel@gmail.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 Brian Nickel
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
using System.Net;
using System.Reflection;
using Mono.FastCgi;
using Mono.WebServer.Log;
using Mono.WebServer.Options;

namespace Mono.WebServer.FastCgi
{
	public static class Server
	{
		delegate bool SocketCreator (ConfigurationManager configmanager, string [] socketParts, out Socket socket);

		static ApplicationServer appserver;

		public static VPathToHost GetApplicationForPath (string vhost,
                                                                 int port,
                                                                 string path,
                                                                 string realPath)
		{
			return appserver.GetApplicationForPath (vhost,	port, path, false);
		}

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

			Logger.Write (LogLevel.Debug,
				Assembly.GetExecutingAssembly ().GetName ().Name);

			Socket socket;
			if (!TryCreateSocket (configurationManager, out socket))
				return 1;

			string root_dir;
			if (!TryGetRootDirectory (configurationManager, out root_dir))
				return 1;

			CreateAppServer (configurationManager, root_dir);

			if (!TryLoadApplicationsConfig (configurationManager))
				return 1;

			Mono.FastCgi.Server server = CreateServer (configurationManager, socket);

			var stoppable = configurationManager.Stoppable;
			server.Start (stoppable, (int)configurationManager.Backlog);
			
			if (stoppable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
				server.Stop ();
			}
			
			return 0;
		}

		static Mono.FastCgi.Server CreateServer (ConfigurationManager configurationManager,
		                                         Socket socket)
		{
			var server = new Mono.FastCgi.Server (socket);

			server.SetResponder (typeof (Responder));

			server.MaxConnections = configurationManager.MaxConns;
			server.MaxRequests = configurationManager.MaxReqs;
			server.MultiplexConnections = configurationManager.Multiplex;

			Logger.Write (LogLevel.Debug, "Max connections: {0}",
				server.MaxConnections);
			Logger.Write (LogLevel.Debug, "Max requests: {0}",
				server.MaxRequests);
			Logger.Write (LogLevel.Debug,
				"Multiplex connections: {0}",
				server.MultiplexConnections);
			return server;
		}

		static void CreateAppServer (ConfigurationManager configurationManager,
		                             string rootDir)
		{
			var webSource = new WebSource ();
			appserver = new ApplicationServer (webSource, rootDir) {
				Verbose = configurationManager.Verbose
			};
		}

		static bool TryLoadApplicationsConfig (ConfigurationManager configurationManager)
		{
			bool autoMap = false; //(bool) configurationManager ["automappaths"];

			var applications = configurationManager.Applications;
			if (applications != null)
				appserver.AddApplicationsFromCommandLine (applications);

			string app_config_file;
			string app_config_dir;

			try {
				app_config_file = configurationManager.AppConfigFile;
				app_config_dir = configurationManager.AppConfigDir;
			} catch (ApplicationException e) {
				Logger.Write (LogLevel.Error, e.Message);
				return false;
			}

			if (app_config_file != null)
				appserver.AddApplicationsFromConfigFile (app_config_file);
			if (app_config_dir != null)
				appserver.AddApplicationsFromConfigDirectory (app_config_dir);

			if (applications == null && app_config_dir == null &&
			    app_config_file == null && !autoMap) {
				Logger.Write (LogLevel.Error,
				              "There are no applications defined, and path mapping is disabled.");
				Logger.Write (LogLevel.Error,
				              "Define an application using /applications, /appconfigfile, /appconfigdir");
				/*
				Logger.Write (LogLevel.Error,
					"or by enabling application mapping with /automappaths=True.");
				*/
				return false;
			}
			return true;
		}

		static bool TryCreateSocket (ConfigurationManager configurationManager, out Socket socket)
		{
			socket = null;

			// Socket strings are in the format
			// "type[:ARG1[:ARG2[:...]]]".
			var socket_type = configurationManager.Socket;

			string[] socket_parts = socket_type.Split (new[] {':'}, 3);

			SocketCreator creator;
			return TryGetSocketCreator (socket_parts, out creator)
				&& creator (configurationManager, socket_parts, out socket);
		}

		static bool TryGetSocketCreator (string[] socket_parts, out SocketCreator creator)
		{
			switch (socket_parts [0].ToLower ()) {
			case "pipe":
				creator = TryCreatePipe;
				return true;
				// The FILE sockets is of the format
				// "file[:PATH]".
			case "unix":
			case "file":
				creator = TryCreateUnixSocket;
				return true;
				// The TCP socket is of the format
				// "tcp[[:ADDRESS]:PORT]".
			case "tcp":
				creator = TryCreateTcpSocket;
				return true;
			default:
				Logger.Write (LogLevel.Error,
				              "Error in argument \"socket\". \"{0}\" is not a supported type. Use \"pipe\", \"tcp\" or \"unix\".",
				              socket_parts [0]);
				creator = null;
				return false;
			}
		}

		static bool TryGetRootDirectory (ConfigurationManager configurationManager,
		                              out string rootDir)
		{
			rootDir = configurationManager.Root;
			if (!String.IsNullOrEmpty (rootDir)) {
				try {
					Environment.CurrentDirectory = rootDir;
				} catch (Exception e) {
					Logger.Write (e);
					return false;
				}
			}
			rootDir = Environment.CurrentDirectory;
			Logger.Write (LogLevel.Debug, "Root directory: {0}",
				rootDir);
			return true;
		}

		static bool TryCreateTcpSocket (ConfigurationManager configurationManager, string[] socketParts, out Socket socket)
		{
			socket = null;
			ushort port;
			try {
				if (socketParts.Length > 1) {
					if (!UInt16.TryParse (socketParts [socketParts.Length - 1], out port)) {
						Logger.Write (LogLevel.Error, "Error parsing port number");
						return false;
					}
				} else {
					port = configurationManager.Port;
				}
			} catch (ApplicationException e) {
				Logger.Write (LogLevel.Error, e.Message);
				return false;
			}
			
			IPAddress address = configurationManager.Address;
			if (socketParts.Length == 3) {
				string address_str = socketParts [1];

				if (address_str == null)
					address = IPAddress.Loopback;
				else if (!IPAddress.TryParse (address_str, out address)) {
					Logger.Write (LogLevel.Error,
					              "Error in argument \"address\". \"{0}\" cannot be converted to an IP address.",
					              address_str);
					return false;
				}
			}

			try {
				socket = SocketFactory.CreateTcpSocket (
					address, port);
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error,
					"Error creating the socket: {0}",
					e.Message);
				return false;
			}

			Logger.Write (LogLevel.Debug,
				"Listening on port: {0}", port);
			Logger.Write (LogLevel.Debug,
				"Listening on address: {0}", address.ToString ());
			return true;
		}

		static bool TryCreateUnixSocket (ConfigurationManager configurationManager, string[] socketParts, out Socket socket)
		{
			string path = socketParts.Length == 2
				? socketParts[1]
				: configurationManager.Filename;

			socket = null;

			try {
				socket = SocketFactory.CreateUnixSocket (path);
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error,
					"Error creating the socket: {0}",
					e.Message);
				return false;
			}

			Logger.Write (LogLevel.Debug,
				"Listening on file: {0}", path);
			return true;
		}

		static bool TryCreatePipe (ConfigurationManager configurationManager, string[] socketParts, out Socket socket)
		{
			socket = null;
			try {
				socket = SocketFactory.CreatePipeSocket (
					IntPtr.Zero);
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error,
					"Pipe socket is not bound.");
				Logger.Write (e);
				var errorcode = e.SocketErrorCode;
				Logger.Write (LogLevel.Debug,
					"Errorcode: {0}", errorcode);
				return false;
			} catch (NotSupportedException) {
				Logger.Write (LogLevel.Error,
					"Error: Pipe sockets are not supported on this system.");
				return false;
			}
			return true;
		}
	}
}
