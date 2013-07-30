using System;
using Mono.WebServer.Log;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		protected ConfigurationManager ()
		{
			settings = new SettingsCollection {help, version, verbose, printlog,
			logFile, configFile,
			loglevels};
		}

		#region Baking fields
		readonly BoolSetting help = new BoolSetting ("help", "Shows this help message and exits.", prototype: "?|h|help");
		readonly BoolSetting version = new BoolSetting ("version", "Displays version information and exits.", "V|version");
		readonly BoolSetting verbose = new BoolSetting ("verbose", "Prints extra messages. Mainly useful for debugging.", prototype: "v|verbose");
		readonly BoolSetting printlog = new BoolSetting ("printlog", "Prints log messages to the console.", environment: "MONO_PRINTLOG|MONO_FCGI_PRINTLOG", defaultValue: true);

		readonly StringSetting logFile = new StringSetting ("logfile", "Specifies a file to log events to.", "FastCgiLogFile", "MONO_LOGFILE|MONO_FCGI_LOGFILE");
		readonly StringSetting configFile = new StringSetting ("configfile|config-file", Descriptions.ConfigFile);

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
