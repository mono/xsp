//
// ConfigurationManager.Fields.cs
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
using Mono.WebServer.Log;
using Mono.WebServer.Options.Settings;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		protected ConfigurationManager (string name)
		{
			settings = new SettingsCollection {help, version, verbose, printlog,
			logFile, configFile, this.name,
			loglevels};
			this.name.MaybeUpdate (SettingSource.Default, name);
		}

		#region Baking fields
		readonly BoolSetting help = new BoolSetting ("help", "Shows this help message and exits.", prototype: "?|h|help");
		readonly BoolSetting version = new BoolSetting ("version", "Displays version information and exits.", "V|version");
		readonly BoolSetting verbose = new BoolSetting ("verbose", "Prints extra messages. Mainly useful for debugging.", prototype: "v|verbose");
		readonly BoolSetting printlog = new BoolSetting ("printlog", "Prints log messages to the console.", environment: "MONO_PRINTLOG|MONO_FCGI_PRINTLOG", defaultValue: true);

		readonly StringSetting logFile = new StringSetting ("logfile", "Specifies a file to log events to.", "FastCgiLogFile", "MONO_LOGFILE|MONO_FCGI_LOGFILE");
		readonly StringSetting configFile = new StringSetting ("configfile|config-file", Descriptions.ConfigFile);
		readonly StringSetting name = new StringSetting ("name", "Specifies a name to print in the log");

		readonly EnumSetting<LogLevel> loglevels = new EnumSetting<LogLevel> ("loglevels", Descriptions.LogLevels, "FastCgiLogLevels", "MONO_FCGI_LOGLEVELS", LogLevel.Standard);
		#endregion

		#region Typesafe properties
		public bool Help {
			get { return help; }
		}
		public bool Version {
			get { return version; }
		}
		public bool Verbose {
			get { return verbose; }
		}
		public bool PrintLog {
			get { return printlog; }
		}

		public string LogFile {
			get { return logFile; }
		}
		public string ConfigFile {
			get { return configFile; }
		}
		public string Name {
			get { return name; }
		}

		public LogLevel LogLevels {
			get { return loglevels; }
		}
		#endregion

		protected void Add (params ISetting[] settings)
		{
			if (settings == null) 
				throw new ArgumentNullException ("settings");
			foreach (ISetting setting in settings)
				this.settings.Add (setting);
		}

		public void SetupLogger ()
		{
			Logger.Level = LogLevels;
			OpenLogFile ();
			Logger.WriteToConsole = PrintLog;
			Logger.Verbose = Verbose;
			Logger.Name = Name;
		}

		void OpenLogFile ()
		{
			try {
				if (LogFile != null)
					Logger.Open (LogFile);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error, "Error opening log file: {0}", e.Message);
				Logger.Write (LogLevel.Warning, "Events will not be logged to file.");
			}
		}

		/// <summary>
		/// If a configfile option was specified, tries to load
		/// the configuration file
		/// </summary>
		/// <returns>false on failure, true on success or
		/// option not present</returns>
		public bool LoadConfigFile ()
		{
			try {
				if (ConfigFile != null)
					if(!TryLoadXmlConfig (ConfigFile))
						return false;
			} catch (ApplicationException e) {
				Logger.Write (LogLevel.Error, e.Message);
				return false;
			} catch (System.Xml.XmlException e) {
				Logger.Write (LogLevel.Error,
					"Error reading XML configuration: {0}",
					e.Message);
				return false;
			}
			return true;
		}
	}
}
