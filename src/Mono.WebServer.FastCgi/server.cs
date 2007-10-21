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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Hosting;
using Mono.WebServer;
using Mono.FastCgi;

namespace Mono.WebServer.FastCgi
{
	public class Server
	{
		static void ShowVersion ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string version = assembly.GetName ().Version.ToString ();
			object att;
			
			// att = assembly.GetCustomAttributes (
			//	typeof (AssemblyTitleAttribute), false) [0];
			// string title =
			//	((AssemblyTitleAttribute) att).Title;
			
			att = assembly.GetCustomAttributes (
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

		static void ShowHelp ()
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

		private static ApplicationManager appmanager;
		private static ConfigurationManager configmanager;
		
		public static ApplicationHost GetApplicationForPath (string vhost,
		                                                     int port,
		                                                     string path,
		                                                     string realPath)
		{
			VPathToHost h = appmanager.GetApplicationForPath (vhost,
				port, path, realPath);
			
			return h == null ? null : h.AppHost as ApplicationHost;
		}
		
		public static int Main (string [] args)
		{
			// Load the configuration file stored in the
			// executable's resources.
			configmanager = new ConfigurationManager (
				typeof (Server).Assembly,
				"ConfigurationManager.xml");
			
			configmanager.LoadCommandLineArgs (args);
			
			// Show the help and exit.
			if ((bool) configmanager ["help"] ||
				(bool) configmanager ["?"]) {
				ShowHelp ();
				return 0;
			}
			
			// Show the version and exit.
			if ((bool) configmanager ["version"]) {
				ShowVersion ();
				return 0;
			}
			
			try {
				string config_file = (string)
					configmanager ["configfile"];
				if (config_file != null)
					configmanager.LoadXmlConfig (
						config_file);
			} catch (ApplicationException e) {
				Console.WriteLine (e.Message);
				return 1;
			} catch (System.Xml.XmlException e) {
				Console.WriteLine (
					"Error reading XML configuration: {0}",
					e.Message);
				return 1;
			}
			
			try {
				string log_level = (string)
					configmanager ["loglevels"];
				
				if (log_level != null)
					Logger.Level = (LogLevel)
						Enum.Parse (typeof (LogLevel),
							log_level);
			} catch {
				Console.WriteLine ("Failed to parse log levels.");
				Console.WriteLine ("Using default levels: {0}",
					Logger.Level);
			}
			
			try {
				string log_file = (string)
					configmanager ["logfile"];
				
				if (log_file != null)
					Logger.Open (log_file);
			} catch (Exception e) {
				Console.WriteLine ("Error opening log file: {0}",
					e.Message);
				Console.WriteLine ("Events will not be logged.");
			}
			
			Logger.WriteToConsole = (bool) configmanager ["printlog"];

			// Send the trace to the console.
			Trace.Listeners.Add (
				new TextWriterTraceListener (Console.Out));
			Console.WriteLine (
				Assembly.GetExecutingAssembly ().GetName ().Name);
			
			
			// Create the socket.
			Socket socket;
			
			// Socket strings are in the format
			// "type[:ARG1[:ARG2[:...]]]".
			string socket_type = configmanager ["socket"] as string;
			if (socket_type == null)
				socket_type = "pipe";
			
			string [] socket_parts = socket_type.Split (
				new char [] {':'}, 3);
			
			switch (socket_parts [0].ToLower ()) {
			case "pipe":
				try {
					socket = SocketFactory.CreatePipeSocket (
						IntPtr.Zero);
				} catch (System.Net.Sockets.SocketException){
					Console.WriteLine (
						"Error: Pipe socket is not bound.");
					return 1;
				}
				break;
			
			// The FILE sockets is of the format
			// "file[:PATH]".
			case "unix":
			case "file":
				if (socket_parts.Length == 2)
					configmanager ["filename"] =
						socket_parts [1];
				
				string path = (string) configmanager ["filename"];
				
				try {
					socket = SocketFactory.CreateUnixSocket (
						path);
				} catch (System.Net.Sockets.SocketException e){
					Console.WriteLine (
						"Error creating the socket: {0}",
						e.Message);
					return 1;
				}
				
				Console.WriteLine ("Listening on file: {0}",
					path);
				break;
			
			// The TCP socket is of the format
			// "tcp[[:ADDRESS]:PORT]".
			case "tcp":
				if (socket_parts.Length > 1)
					configmanager ["port"] = socket_parts [
						socket_parts.Length - 1];
				
				if (socket_parts.Length == 3)
					configmanager ["address"] =
						socket_parts [1];
				
				ushort port;
				try {
					port = (ushort) configmanager ["port"];
				} catch (ApplicationException e) {
					Console.WriteLine (e.Message);
					return 1;
				}
				
				string address_str =
					(string) configmanager ["address"];
				IPAddress address;
				
				try {
					address = IPAddress.Parse (address_str);
				} catch {
					Console.WriteLine (
						"Error in argument \"address\". \"{0}\" cannot be converted to an IP address.",
						address_str);
					return 1;
				}
				
				try {
					socket = SocketFactory.CreateTcpSocket (
						address, port);
				} catch (System.Net.Sockets.SocketException e){
					Console.WriteLine (
						"Error creating the socket: {0}",
						e.Message);
					return 1;
				}
				
				Console.WriteLine ("Listening on port: {0}",
					address_str);
				Console.WriteLine ("Listening on address: {0}",
					port);
				break;
				
			default:
				Console.WriteLine (
					"Error in argument \"socket\". \"{0}\" is not a supported type. Use \"pipe\", \"tcp\" or \"unix\".",
					socket_parts [0]);
				return 1;
			}
			
			string root_dir = configmanager ["root"] as string;
			if (root_dir != null && root_dir.Length != 0) {
				try {
					Environment.CurrentDirectory = root_dir;
				} catch (Exception e) {
					Console.WriteLine ("Error: {0}",
						e.Message);
					return 1;
				}
			}
			
			root_dir = Environment.CurrentDirectory;
			bool auto_map = (bool) configmanager ["automappaths"];
			appmanager = new ApplicationManager (
				typeof (ApplicationHost), auto_map, false);
			appmanager.Verbose = (bool) configmanager ["verbose"];
			
			string applications = (string)
				configmanager ["applications"];
			string app_config_file;
			string app_config_dir;
			
			try {
				app_config_file = (string)
					configmanager ["appconfigfile"];
				app_config_dir = (string)
					configmanager ["appconfigdir"];
			} catch (ApplicationException e) {
				Console.WriteLine (e.Message);
				return 1;
			}
			
			if (applications != null)
				appmanager.AddApplicationsFromCommandLine (
					applications);
			
			if (app_config_file != null)
				appmanager.AddApplicationsFromConfigFile (
					app_config_file);
			
			if (app_config_dir != null)
				appmanager.AddApplicationsFromConfigDirectory (
					app_config_dir);

			if (applications == null && app_config_dir == null &&
				app_config_file == null && !auto_map) {
				Console.WriteLine (
					"There are no applications defined, and path mapping is disabled.");
				Console.WriteLine (
					"Define an application using /applications, /appconfigfile, /appconfigdir");
				Console.WriteLine (
					"or by enabling application mapping with /automappaths=True.");
				return 1;
			}
			
			Console.WriteLine ("Root directory: {0}", root_dir);
			Mono.FastCgi.Server server = new Mono.FastCgi.Server (
				socket);
			
			server.SetResponder (typeof (Responder));
			
			server.MaxConnections = (ushort)
				configmanager ["maxconns"];
			server.MaxRequests = (ushort)
				configmanager ["maxreqs"];
			server.MultiplexConnections = (bool)
				configmanager ["multiplex"];
			
			Console.WriteLine ("Max connections: {0}",
				server.MaxConnections);
			Console.WriteLine ("Max requests: {0}",
				server.MaxRequests);
			Console.WriteLine ("Multiplex connections: {0}",
				server.MultiplexConnections);
			
			bool stopable = (bool) configmanager ["stopable"];
			if (!stopable)
				Console.WriteLine (
					"Use /stopable=True to enable stopping from the console.");
			
			server.Start (stopable);
			
			configmanager = null;
			
			if (stopable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
				server.Stop ();
			}
			
			return 0;
		}
	}
}
