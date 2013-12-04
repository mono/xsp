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
using System.Timers;
using Mono.WebServer.Log;
using Mono.WebServer.Options;
using Mono.FastCgi;
using Mono.WebServer.FastCgi.Sockets;
using Mono.Unix.Native;

namespace Mono.WebServer.FastCgi {
	public static class Server
	{
		const string WRONG_KIND = "Error in argument \"socket\". \"{0}\" is not a supported type. Use \"pipe\", \"tcp\" or \"unix\".";

		delegate bool SocketCreator (ConfigurationManager configmanager,string[] socketParts,out Socket socket);

		static ApplicationServer appserver;

		public static VPathToHost GetApplicationForPath (string vhost,
		                                                 int port,
		                                                 string path,
		                                                 string realPath)
		{
			return appserver.GetApplicationForPath (vhost,	port, path, false);
		}

		public static int Main (string[] args)
		{
			if (Platform.IsUnix) {
				uint uid = Syscall.geteuid ();
				uint gid = Syscall.getegid ();
				Platform.SetIdentity (uid, gid);
			}

			var configurationManager = new ConfigurationManager ("fastcgi-mono-server");
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
			Platform.LogIdentity ();

			string root_dir;
			if (!TryGetRootDirectory (configurationManager, out root_dir))
				return 1;

			CreateAppServer (configurationManager, root_dir);

			if (!TryLoadApplicationsConfig (configurationManager))
				return 1;

			IServer server;

			if (configurationManager.OnDemand) {
				Socket socket;
				Uri uri;
				if (!TryCreateOnDemandSocket (configurationManager.OnDemandSock, out socket, out uri))
					return 1;
				server = CreateOnDemandServer (configurationManager, socket);
				CreateWatchdog (configurationManager, server);
			} else {
				Socket socket;
				if (!TryCreateSocket (configurationManager, out socket))
					return 1;
				server = new ServerProxy (CreateServer (configurationManager, socket));
			}

			var stoppable = configurationManager.Stoppable;
			if (!server.Start (stoppable, (int)configurationManager.Backlog)) {
				Logger.Write (LogLevel.Error, "Failed to start server!");
				return 1;
			}
			
			if (stoppable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
				server.Stop ();
			}
			
			return 0;
		}

		static bool TryCreateOnDemandSocket (string onDemandSock, out Socket socket, out Uri uri)
		{
			socket = null;
			return (Uri.TryCreate (onDemandSock, UriKind.Absolute, out uri) && TryCreateSocketFromUri (uri, out socket))
				|| TryCreateUnixSocket (onDemandSock, out socket);
		}

		static void CreateWatchdog (ConfigurationManager configurationManager, IServer server)
		{
			using (var aliveLock = new System.Threading.ReaderWriterLockSlim ()) {
				bool alive = false;

				// On a new connection try to set alive to true
				// If we can't then don't bother, it's not needed
				server.RequestReceived += (sender, e) =>
					TryRunLocked (
					() => aliveLock.TryEnterWriteLock (0),
					() => alive = true,
					aliveLock.ExitWriteLock
				);

				var pluto = new Watchdog (configurationManager.IdleTime * 1000);
				pluto.End += (sender, e) => {
					Logger.Write (LogLevel.Debug, "The dog bit!");
					server.Stop ();
					Logger.Write (LogLevel.Debug, "Server stopped.");
					Logger.Write (LogLevel.Debug, "Goodbye!");
					Environment.Exit (0);
				};

				// Check every second for hearthbeats
				var t = new Timer (1000);
				t.Elapsed += (sender, e) =>
					RunLocked (
					aliveLock.EnterUpgradeableReadLock,
					() => {
						if (!alive)
							return;
						RunLocked (
							aliveLock.EnterWriteLock,
							() => alive = false,
							aliveLock.ExitWriteLock
						);
						pluto.Kick ();
					},
					aliveLock.ExitUpgradeableReadLock
				);
				t.Start ();
			}
		}

		static void RunLocked (Action takeLock, Action code, Action releaseLock)
		{
			if (takeLock == null)
				throw new ArgumentNullException ("takeLock");
			if (code == null)
				throw new ArgumentNullException ("code");
			if (releaseLock == null)
				throw new ArgumentNullException ("releaseLock");
			TryRunLocked (() => {
				takeLock ();
				return true;
			}, code, releaseLock);
		}

		static void TryRunLocked (Func<bool> takeLock, Action code, Action releaseLock)
		{
			if (takeLock == null)
				throw new ArgumentNullException ("takeLock");
			if (code == null)
				throw new ArgumentNullException ("code");
			if (releaseLock == null)
				throw new ArgumentNullException ("releaseLock");
			bool locked = false;
			try {
				if (!takeLock ())
					return;
				locked = true;
				code ();
			} finally {
				if (locked)
					releaseLock ();
			}
		}

		static IServer CreateOnDemandServer (ConfigurationManager configurationManager, Socket socket)
		{
			if (configurationManager == null)
				throw new ArgumentNullException ("configurationManager");
			if (socket == null)
				throw new ArgumentNullException ("socket");
			var server = new OnDemandServer (socket) {
				MaxConnections = configurationManager.MaxConns,
				MaxRequests = configurationManager.MaxReqs,
				MultiplexConnections = configurationManager.Multiplex
			};

			server.SetResponder (typeof(Responder));

			Logger.Write (LogLevel.Debug, "Max connections: {0}", server.MaxConnections);
			Logger.Write (LogLevel.Debug, "Max requests: {0}", server.MaxRequests);
			Logger.Write (LogLevel.Debug, "Multiplex connections: {0}", server.MultiplexConnections);
			return server;
		}

		static Mono.FastCgi.Server CreateServer (ConfigurationManager configurationManager,
		                                         Socket socket)
		{
			var server = new Mono.FastCgi.Server (socket) {
				MaxConnections = configurationManager.MaxConns,
				MaxRequests = configurationManager.MaxReqs,
				MultiplexConnections = configurationManager.Multiplex
			};

			server.SetResponder (typeof(Responder));

			Logger.Write (LogLevel.Debug, "Max connections: {0}", server.MaxConnections);
			Logger.Write (LogLevel.Debug, "Max requests: {0}", server.MaxRequests);
			Logger.Write (LogLevel.Debug, "Multiplex connections: {0}", server.MultiplexConnections);
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

		public static bool TryCreateSocket (ConfigurationManager configurationManager, out Socket socket)
		{
			socket = null;

			// Socket strings are in the format
			// "type[:ARG1[:ARG2[:...]]]".
			var socket_type = configurationManager.Socket;

			Uri uri;
			if (Uri.TryCreate (socket_type, UriKind.Absolute, out uri)) {
				Logger.Write (LogLevel.Debug, "Parsed {0} as URI {1}", socket_type, uri);
				if (TryCreateSocketFromUri (uri, out socket)) {
					Logger.Write (LogLevel.Debug, "Created socket {0} from URI {1}", socket, uri);
					return true;
				}
			}

			string[] socket_parts = socket_type.Split (new [] { ':' }, 3);

			SocketCreator creator;
			return TryGetSocketCreator (socket_parts [0], out creator)
			&& creator (configurationManager, socket_parts, out socket);
		}

		static bool TryCreateSocketFromUri (Uri uri, out Socket socket)
		{
			socket = null;
			
			switch (uri.Scheme) {
				case "pipe":
					return TryCreatePipe (out socket);
				case "unix":
				case "file":
					if (String.IsNullOrEmpty (uri.PathAndQuery))
						throw new ArgumentException (String.Format ("Path is null in \"{0}\"", uri));
					return TryCreateUnixSocket (uri.PathAndQuery, out socket, uri.UserInfo);
				case "tcp":
					return TryCreateTcpSocket (uri.Host, uri.Port, out socket);
				default:
					Logger.Write (LogLevel.Error, WRONG_KIND, uri.Scheme);
					return false;
			}
		}

		static bool TryGetSocketCreator (string socket_kind, out SocketCreator creator)
		{
			switch (socket_kind.ToLower ()) {
				case "pipe":
					creator = (ConfigurationManager configmanager, string[] socketParts, out Socket socket) => TryCreatePipe (out socket);
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
					Logger.Write (LogLevel.Error, WRONG_KIND, socket_kind);
					creator = null;
					return false;
			}
		}

		static bool TryGetRootDirectory (ConfigurationManager configurationManager, out string rootDir)
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
			Logger.Write (LogLevel.Debug, "Root directory: {0}", rootDir);
			return true;
		}

		static bool TryCreateTcpSocket (string ip, int port, out Socket socket)
		{
			socket = null;

			IPAddress address;
			if (!IPAddress.TryParse (ip, out address))
				return false;

			socket = new TcpSocket (address, port);
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
				socket = new TcpSocket (address, port);
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error, "Error creating the socket: {0}", e.Message);
				return false;
			}

			Logger.Write (LogLevel.Debug, "Listening on port: {0}", port);
			Logger.Write (LogLevel.Debug, "Listening on address: {0}", address);
			return true;
		}

		static bool TryCreateUnixSocket (ConfigurationManager configurationManager, string[] socketParts, out Socket socket)
		{
			string path = socketParts.Length == 2
				? socketParts [1]
				: configurationManager.Filename;

			return TryCreateUnixSocket (path, out socket);
		}

		public static bool TryCreateUnixSocket (string path, out Socket socket, string perm = null)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			if (path.Length == 0)
				throw new ArgumentException ("path must not be empty", "path");
			socket = null;
			try {
				string realPath;
				if (path.StartsWith ("\\0", StringComparison.Ordinal) && path.IndexOf ("\\0", 1, StringComparison.Ordinal) < 0)
					realPath = '\0' + path.Substring (2);
				else
					realPath = path;

				if (perm == null) {
					socket = new UnixSocket (realPath);
					Logger.Write (LogLevel.Debug, "Listening on file {0} with default permissions", realPath);
				} else {
					ushort uperm;
					if (!UInt16.TryParse (perm, out uperm)) {
						Logger.Write (LogLevel.Error, "Error parsing permissions \"{0}\". Use octal.", perm);
						return false;
					}
					uperm = Convert.ToUInt16 (uperm.ToString (), 8);
					socket = new UnixSocket (realPath, uperm);
					Logger.Write (LogLevel.Debug, "Listening on file {0} with permissions {1}", realPath, Convert.ToString (uperm, 8));
				}
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error, "Error creating the socket: {0}", e.Message);
				return false;
			}
			return true;
		}

		static bool TryCreatePipe (out Socket socket)
		{
			socket = null;
			try {
				socket = new UnmanagedSocket (IntPtr.Zero);
			} catch (System.Net.Sockets.SocketException e) {
				Logger.Write (LogLevel.Error, "Pipe socket is not bound.");
				Logger.Write (e);
				var errorcode = e.SocketErrorCode;
				Logger.Write (LogLevel.Debug, "Errorcode: {0}", errorcode);
				return false;
			} catch (NotSupportedException) {
				Logger.Write (LogLevel.Error, "Error: Pipe sockets are not supported on this system.");
				return false;
			}
			return true;
		}
	}
}
