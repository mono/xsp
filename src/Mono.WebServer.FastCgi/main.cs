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
using System.IO;
using System.Net;
using System.Reflection;
using Mono.FastCgi;

namespace Mono.WebServer.FastCgi
{
	public class Server
	{
		static void ShowVersion ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string version = assembly.GetName ().Version.ToString ();
			
			// att = assembly.GetCustomAttributes (
			//	typeof (AssemblyTitleAttribute), false) [0];
			// string title =
			//	((AssemblyTitleAttribute) att).Title;
			
			object att = assembly.GetCustomAttributes (
				typeof (AssemblyCopyrightAttribute), false) [0];
			string copyright =
				((AssemblyCopyrightAttribute) att).Copyright;
			
			att = assembly.GetCustomAttributes (
				typeof (AssemblyDescriptionAttribute), false) [0];
			string description =
				((AssemblyDescriptionAttribute) att).Description;
			
			Console.WriteLine ("{0} {1}\n(c) {2}\n{3}",
				Path.GetFileName (assembly.Location), version,
				copyright, description);
		}

		static void ShowHelp (ConfigurationManager configmanager)
		{
			string name = Path.GetFileName (
				Assembly.GetExecutingAssembly ().Location);
			
			ShowVersion ();
			Console.WriteLine ();
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    {0} [...]", name);
			Console.WriteLine ();
			configmanager.PrintHelp ();
		}

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
			// Load the configuration file stored in the
			// executable's resources.
			var configmanager = new ConfigurationManager (
				typeof (Server).Assembly,
				"ConfigurationManager.xml");
			
			configmanager.LoadCommandLineArgs (args);
			
			// Show the help and exit.
			if ((bool) configmanager ["help"] ||
				(bool) configmanager ["?"]) {
				ShowHelp (configmanager);
				return 0;
			}
			
			// Show the version and exit.
			if ((bool) configmanager ["version"]) {
				ShowVersion ();
				return 0;
			}

			// Enable console logging during Main ().
			Logger.WriteToConsole = true;

			if (!LoadConfigFile(configmanager))
				return 1;

#if DEBUG
			// Log everything while debugging
			Logger.Level = LogLevel.All;
#endif

			SetLogLevel(configmanager);

			OpenLogFile (configmanager);

			Logger.Write (LogLevel.Debug,
				Assembly.GetExecutingAssembly ().GetName ().Name);


			Socket socket;
			if (!CreateSocket (configmanager, out socket))
				return 1;

			string root_dir;
			if (!GetRootDirectory (configmanager, out root_dir))
				return 1;

			CreateAppServer (configmanager, root_dir);

			if (!LoadApplicationsConfig (configmanager))
				return 1;

			Mono.FastCgi.Server server = CreateServer (configmanager,
				socket);

			Logger.WriteToConsole = (bool)configmanager ["printlog"];
			
			var stoppable = (bool) configmanager ["stoppable"];
			server.Start (stoppable);
			
			if (stoppable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
				server.Stop ();
			}
			
			return 0;
		}

		static Mono.FastCgi.Server CreateServer (ConfigurationManager configmanager,
		                                         Socket socket)
		{
			var server = new Mono.FastCgi.Server (socket);

			server.SetResponder (typeof (Responder));

			server.MaxConnections = (ushort) configmanager ["maxconns"];
			server.MaxRequests = (ushort) configmanager ["maxreqs"];
			server.MultiplexConnections = (bool) configmanager ["multiplex"];

			Logger.Write (LogLevel.Debug, "Max connections: {0}",
				server.MaxConnections);
			Logger.Write (LogLevel.Debug, "Max requests: {0}",
				server.MaxRequests);
			Logger.Write (LogLevel.Debug,
				"Multiplex connections: {0}",
				server.MultiplexConnections);
			return server;
		}

		static void CreateAppServer (ConfigurationManager configmanager,
		                             string rootDir)
		{
			var webSource = new WebSource ();
			appserver = new ApplicationServer (webSource, rootDir) {
				Verbose = (bool) configmanager ["verbose"]
			};
		}

		static bool LoadApplicationsConfig (ConfigurationManager configmanager)
		{
			bool autoMap = false; //(bool) configmanager ["automappaths"];

			var applications = configmanager ["applications"] as string;
			string app_config_file;
			string app_config_dir;

			try {
				app_config_file = configmanager ["appconfigfile"]
					as string;
				app_config_dir = configmanager ["appconfigdir"]
					as string;
			} catch (ApplicationException e) {
				Logger.Write (LogLevel.Error, e.Message);
				return false;
			}

			if (applications != null) {
				appserver.AddApplicationsFromCommandLine (
					applications);
			}

			if (app_config_file != null) {
				appserver.AddApplicationsFromConfigFile (
					app_config_file);
			}

			if (app_config_dir != null) {
				appserver.AddApplicationsFromConfigDirectory (
					app_config_dir);
			}

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

		static bool CreateSocket (ConfigurationManager configmanager,
		                          out Socket socket)
		{
			socket = null;

			// Socket strings are in the format
			// "type[:ARG1[:ARG2[:...]]]".
			var socket_type = configmanager ["socket"] as string
				?? "pipe";

			string[] socket_parts = socket_type.Split (
				new[] {':'}, 3);

			switch (socket_parts [0].ToLower ()) {
			case "pipe":
				return CreatePipe (ref socket);

			// The FILE sockets is of the format
			// "file[:PATH]".
			case "unix":
			case "file":
				return CreateUnixSocket (configmanager, 
					socket_parts, ref socket);

			// The TCP socket is of the format
			// "tcp[[:ADDRESS]:PORT]".
			case "tcp":
				return CreateTcpSocket (configmanager,
					socket_parts, ref socket);

			default:
				Logger.Write (LogLevel.Error,
					"Error in argument \"socket\". \"{0}\" is not a supported type. Use \"pipe\", \"tcp\" or \"unix\".",
					socket_parts [0]);
				return false;
			}
		}

		static bool GetRootDirectory (ConfigurationManager configmanager,
		                              out string rootDir)
		{
			rootDir = configmanager ["root"] as string;
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

		static bool CreateTcpSocket (ConfigurationManager configmanager,
		                             string [] socketParts,
			                     ref Socket socket)
		{
			if (socketParts.Length > 1)
				configmanager ["port"] = socketParts [
					socketParts.Length - 1];

			if (socketParts.Length == 3)
				configmanager ["address"] = socketParts [1];

			ushort port;
			try {
				port = (ushort) configmanager ["port"];
			} catch (ApplicationException e) {
				Logger.Write (LogLevel.Error, e.Message);
				return false;
			}

			var address_str = configmanager ["address"] as string;
			IPAddress address;

			if (address_str == null)
				address = IPAddress.Loopback;
			else if(!IPAddress.TryParse (address_str,out address)) {
				Logger.Write (LogLevel.Error,
					"Error in argument \"address\". \"{0}\" cannot be converted to an IP address.",
					address_str);
				return false;
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
				"Listening on port: {0}", address_str);
			Logger.Write (LogLevel.Debug,
				"Listening on address: {0}", port);
			return true;
		}

		static bool CreateUnixSocket (ConfigurationManager configmanager,
		                              string [] socketParts,
		                              ref Socket socket)
		{
			if (socketParts.Length == 2)
				configmanager ["filename"] = socketParts [1];

			var path = configmanager ["filename"] as string;

			try {
				socket = SocketFactory.CreateUnixSocket (
					path);
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

		static bool CreatePipe (ref Socket socket)
		{
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

		static void OpenLogFile (ConfigurationManager configmanager)
		{
			try {
				var log_file = configmanager ["logfile"]
					as string;

				if (log_file != null)
					Logger.Open (log_file);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error,
					"Error opening log file: {0}",
					e.Message);
				Logger.Write (LogLevel.Warning,
					"Events will not be logged to file.");
			}
		}

		static void SetLogLevel(ConfigurationManager configmanager)
		{
			var log_level = configmanager["loglevels"] as string;

			if (log_level == null)
				return;

			LogLevel level;
			if (Enum.TryParse(log_level, true, out level)) {
				Logger.Level = level;
			}
			else {
				Logger.Write(LogLevel.Warning,
					"Failed to parse log levels.");
				Logger.Write(LogLevel.Notice,
					"Using default levels: {0}",
					Logger.Level);
			}
		}

		/// <summary>
		/// If a configfile option was specified, tries to load
		/// the configuration file
		/// </summary>
		/// <returns>false on failure, true on success or
		/// option not present</returns>
		static bool LoadConfigFile(ConfigurationManager configmanager)
		{
			try {
				var config_file = configmanager["configfile"]
					as string;
				if (config_file != null)
					configmanager.LoadXmlConfig(
						config_file);
			}
			catch (ApplicationException e) {
				Logger.Write(LogLevel.Error, e.Message);
				return false;
			}
			catch (System.Xml.XmlException e)
			{
				Logger.Write(LogLevel.Error,
					"Error reading XML configuration: {0}",
					e.Message);
				return false;
			}
			return true;
		}
	}
}
