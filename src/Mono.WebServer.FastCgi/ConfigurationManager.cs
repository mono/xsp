//
// ConfigurationManager.cs: Generic multi-source configuration manager.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Robert Jordan <robertj@gmx.net>
//
// Copyright (C) 2007 Brian Nickel
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
using System.Xml;
using System.Globalization;
using Mono.WebServer.FastCgi.Configuration;
using Mono.FastCgi;

namespace Mono.WebServer.FastCgi {
	public partial class ConfigurationManager {
		readonly SettingsCollection settings;

		public ConfigurationManager ()
		{
			settings = new SettingsCollection {help, version, verbose, printlog, stoppable, multiplex, maxConns, maxReqs, port,
			address, filename, logFile, configFile, root, appConfigFile, appConfigDir, socket, applications, loglevels};
		}

		#region Backing fields
		readonly BoolSetting help = new BoolSetting ("help","Shows this help message and exits.", prototype: "?|h|help");
		readonly BoolSetting version = new BoolSetting ("version","Displays version information and exits.");
		readonly BoolSetting verbose = new BoolSetting ("verbose", "Prints extra messages. Mainly useful for debugging.", prototype: "v|verbose");
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
		readonly StringSetting root = new StringSetting ("root", Descriptions.Root, "MonoServerRootDir", "MONO_FCGI_ROOT");
		readonly StringSetting appConfigFile = new StringSetting ("appconfigfile", Descriptions.AppConfigFile, "MonoApplicationsConfigFile",
			"MONO_FCGI_APPCONFIGFILE");
		readonly StringSetting appConfigDir = new StringSetting ("appconfigdir", "Adds application definitions from all XML files" +
			" found in the specified directory. Files must have the \".webapp\" extension.", "MonoApplicationsConfigDir",
			"MONO_FCGI_APPCONFIGDIR");
		readonly StringSetting socket = new StringSetting ("socket", Descriptions.Socket, "MonoSocketType", "MONO_FCGI_SOCKET", "pipe");
		readonly StringSetting applications = new StringSetting ("applications", Descriptions.Applications, "MonoApplications", "MONO_FCGI_APPLICATIONS");

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
		public bool Help { get { return help; } }
		public bool Version { get { return version; } }
		public bool Verbose { get { return verbose; } }
		public bool PrintLog { get { return printlog; } }
		public bool Stoppable { get { return stoppable; } }
		public bool Multiplex { get { return multiplex; } }

		public ushort MaxConns { get { return maxConns; } }
		public ushort MaxReqs { get { return maxReqs; } }
		public ushort Port { get { return port; } }

		public string Address { get { return address; } }
		public string Filename { get { return filename; } }
		public string LogFile { get { return logFile; } }
		public string ConfigFile { get { return configFile; } }
		public string Root { get { return root; } }
		public string AppConfigFile { get { return appConfigFile; } }
		public string AppConfigDir { get { return appConfigDir; } }
		public string Socket { get { return socket; } }
		public string Applications { get { return applications; } }

		public LogLevel LogLevels { get { return loglevels; } }

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
		
		internal static ApplicationException AppExcept (string message,
							params object [] args)
		{
			return new ApplicationException (String.Format (
				CultureInfo.InvariantCulture, message, args));
		}

		static string GetXmlValue (XmlElement elem, string name)
		{
			string value = elem.GetAttribute (name);
			if (!String.IsNullOrEmpty (value))
				return value;

			foreach (XmlElement child in elem.GetElementsByTagName (name)) {
				value = child.InnerText;
				if (!String.IsNullOrEmpty (value))
					return value;
			}

			return String.Empty;
		}

		[Obsolete]
		internal void SetValue (string name, object value)
		{
			settings[name].MaybeParseUpdate (SettingSource.CommandLine, value.ToString ());
		}

		internal const string ExceptUnregistered =
			"Argument \"{0}\" is unknown.";

		internal const string ExceptDirectory =
			"Error in argument \"{0}\". \"{1}\" is not a directory or does not exist.";

		internal const string ExceptFile =
			"Error in argument \"{0}\". \"{1}\" does not exist.";

		internal const string ExceptUnknown =
			"The Argument \"{0}\" has an invalid type: {1}.";

		[Obsolete]
		internal ISetting GetSetting (string name)
		{
			return settings [name];
		}

		public void PrintHelp ()
		{
			CreateOptionSet ().WriteOptionDescriptions (Console.Out);
		}
	}
}
