//
// ConfigurationManager.cs: Generic multi-source configuration manager.
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialene@gmail.com>
//
// Copyright (C) 2013 Leonardo Taglialegne
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
using Mono.FastCgi;
using Mono.WebServer.Options;

namespace Mono.WebServer.FastCgi {
	public partial class ConfigurationManager : Options.ConfigurationManager
	{
		public ConfigurationManager ()
		{
			Add(printlog, stoppable, multiplex, maxConns, maxReqs, port,
			address, filename, logFile, configFile, appConfigFile, appConfigDir, socket, loglevels);
		}

		#region Backing fields
		readonly BoolSetting printlog = new BoolSetting ("printlog", "Prints log messages to the console.", environment: "MONO_FCGI_PRINTLOG");
		readonly BoolSetting stoppable = new BoolSetting ("stoppable", Descriptions.Stoppable);
		readonly BoolSetting multiplex = new BoolSetting ("multiplex", "Allows multiple requests to be send over a single connection.",
			"FastCgiMultiplexConnections", "MONO_FCGI_MULTIPLEX");

		readonly UInt16Setting maxConns = new UInt16Setting ("maxconns", Descriptions.MaxConns,
			"FastCgiMaxConnections", "MONO_FCGI_MAXCONNS", 1024);
		readonly UInt16Setting maxReqs = new UInt16Setting ("maxreqs", "Specifies the maximum number of concurrent requests the server should accept.",
			"FastCgiMaxRequests", "MONO_FCGI_MAXREQS", 1024);
		readonly UInt16Setting port = new UInt16Setting ("port", "Specifies the TCP port number to listen on.\n" +
			"To use this argument, \"socket\" must be set to \"tcp\".", "MonoServerPort", "MONO_FCGI_PORT", 9000);

		readonly StringSetting address = new StringSetting ("address", "Specifies the IP address to listen on.\n" +
			"To use this argument, \"socket\" must be set to \"tcp\".", "MonoServerAddress", "MONO_FCGI_ADDRESS", "127.0.0.1");
		readonly StringSetting filename = new StringSetting ("filename", "Specifies a unix socket filename to listen on.\n" +
			"To use this argument, \"socket\" must be set to \"unix\".", "MonoUnixSocket", "MONO_FCGI_FILENAME", "/tmp/fastcgi-mono-server");
		readonly StringSetting logFile = new StringSetting ("logfile", "Specifies a file to log events to.", "FastCgiLogFile", "MONO_FCGI_LOGFILE");
		readonly StringSetting configFile = new StringSetting ("configfile", Descriptions.ConfigFile);
		readonly StringSetting appConfigFile = new StringSetting ("appconfigfile", Descriptions.AppConfigFile, "MonoApplicationsConfigFile",
			"MONO_FCGI_APPCONFIGFILE");
		readonly StringSetting appConfigDir = new StringSetting ("appconfigdir", "Adds application definitions from all XML files" +
			" found in the specified directory. Files must have the \".webapp\" extension.", "MonoApplicationsConfigDir",
			"MONO_FCGI_APPCONFIGDIR");
		readonly StringSetting socket = new StringSetting ("socket", Descriptions.Socket, "MonoSocketType", "MONO_FCGI_SOCKET", "pipe");

		readonly Setting<LogLevel> loglevels = new Setting<LogLevel> ("loglevels",
#if NET_4_0
			Enum.TryParse,
#else
			LogLevelParser,
#endif
			Descriptions.LogLevels, "FastCgiLogLevels", "MONO_FCGI_LOGLEVELS");

#if !NET_4_0
		static bool LogLevelParser (string input, out LogLevel output)
		{
			output=LogLevel.Standard;
			try {
				output = (LogLevel) Enum.Parse (typeof (LogLevel), input);
				return true;
			} catch (ArgumentException) { // TODO: catch more specific type
				return false;
			}
		}
#endif
		#endregion

		#region Typesafe properties
		public bool PrintLog {
			get { return printlog; }
		}
		public bool Stoppable {
			get { return stoppable; }
		}
		public bool Multiplex {
			get { return multiplex; }
		}

		public ushort MaxConns {
			get { return maxConns; }
		}
		public ushort MaxReqs {
			get { return maxReqs; }
		}
		public ushort Port {
			get { return port; }
		}

		public string Address {
			get { return address; }
		}
		public string Filename {
			get { return filename; }
		}
		public string LogFile {
			get { return logFile; }
		}
		public string ConfigFile {
			get { return configFile; }
		}
		public string AppConfigFile {
			get { return appConfigFile; }
		}
		public string AppConfigDir {
			get { return appConfigDir; }
		}
		public string Socket {
			get { return socket; }
		}

		public LogLevel LogLevels {
			get { return loglevels; }
		}

		/*
		 * <Setting Name="automappaths" AppSetting="MonoAutomapPaths"
		 * Environment="MONO_FCGI_AUTOMAPPATHS" Type="Bool" ConsoleVisible="True" Value="False">
		 * <Description>
		 * <para>Automatically registers applications as they are
		 * encountered, provided pages exist in standard
		 * locations.</para>
		 * </Description>
		 * </Setting>
		 */
		#endregion
	}
}
