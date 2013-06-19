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
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.Globalization;
using System.Collections.Specialized;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer
{
	public partial class ConfigurationManager
	{
		readonly SettingsInfoCollection settingsInfo = new SettingsInfoCollection ();
		
		readonly IDictionary<string,string> cmd_args = new Dictionary<string,string> ();
		readonly IDictionary<string,string> xml_args = new Dictionary<string,string> ();
		readonly IDictionary<string,string> default_args = new Dictionary<string,string> ();

		ushort maxConnsDefault;
		ushort maxReqsDefault;
		string socketDefault;
		ushort portDefault;

#region Typesafe properties
		public bool Help { get { return GetBool ("help") || GetBool ("?"); } }
		public bool Version { get { return GetBool ("version"); } }
		public bool Verbose { get { return GetBool ("verbose"); } }
		public bool PrintLog { get { return GetBool ("printlog"); } }
		public bool Stoppable { get { return GetBool ("stoppable"); } }
		public ushort MaxConns { get { return TryGetUInt16 ("maxconns") ?? maxConnsDefault; } }
		public ushort MaxReqs { get { return TryGetUInt16 ("maxreqs") ?? maxReqsDefault; } }
		public bool Multiplex { get { return GetBool ("multiplex"); } }
		public string Applications { get { return GetString ("applications"); } }
		public string AppConfigFile { get { return GetString ("appconfigfile"); } }
		public string AppConfigDir { get { return GetString ("appconfigdir"); } }
		public string Socket { get { return GetString ("socket") ?? socketDefault; } }
		public string Root { get { return GetString ("root"); } }
		public ushort Port { get { return TryGetUInt16 ("port") ?? portDefault; } }
		public string Address { get { return GetString ("address"); } }
		public string Filename { get { return GetString ("filename"); } }
		public string LogFile { get { return GetString ("logfile"); } }
		public string LogLevels { get { return GetString ("loglevels"); } }
		public string ConfigFile { get { return GetString ("configfile"); } }
#endregion

		public ConfigurationManager (Assembly asm, string resource)
		{
			var doc = new XmlDocument ();
			doc.Load (asm.GetManifestResourceStream (resource));
			ImportSettings (doc, default_args, false, true);
		}

		SettingInfo GetSettingInfo (string name)
		{
			if (settingsInfo.Contains (name))
				return settingsInfo [name];
			return null;
		}

		string TryGetString (string name, out SettingInfo setting)
		{
			setting = GetSettingInfo (name);
			
			if (setting == null)
				return null;
			
			string value;
			if (cmd_args.TryGetValue(name, out value))
				return value;
			if (xml_args.TryGetValue(name, out value))
				return value;

			string env_setting = setting.Environment;
			if (!String.IsNullOrEmpty (env_setting)){
				value = Environment.GetEnvironmentVariable(
					env_setting);
				if (value != null)
					return value;
			}

			string app_setting = setting.AppSetting;

			if (!String.IsNullOrEmpty (app_setting) &&
				(value = AppSettings [app_setting]) != null)
					return value;

			default_args.TryGetValue(name, out value);
			return value;
		}
		
		public bool Contains (string name)
		{
			string str_value;
			return TryGetString (name, out str_value);
		}

		bool TryGetString (string name, out string value)
		{
			SettingInfo setting;
			value = TryGetString (name, out setting);
			return setting != null;
		}

		const string EXCEPT_UNREGISTERED =
			"Argument \"{0}\" is unknown.";

		const string EXCEPT_UINT16 =
			"Error in argument \"{0}\". \"{1}\" cannot be converted to an integer.";

		const string EXCEPT_BOOL =
			"Error in argument \"{0}\". \"{1}\" should be \"True\" or \"False\".";
		
		const string EXCEPT_DIRECTORY =
			"Error in argument \"{0}\". \"{1}\" is not a directory or does not exist.";
			
		const string EXCEPT_FILE =
			"Error in argument \"{0}\". \"{1}\" does not exist.";
		
		const string EXCEPT_UNKNOWN =
			"The Argument \"{0}\" has an invalid type: {1}.";

		bool GetBool (string name)
		{
			return TryGetBool (name) ?? false;
		}

		bool? TryGetBool (string name)
		{
			string str_value = GetString (name);

			if (str_value == null)
				return null;

			bool value;
			if (!Boolean.TryParse (str_value, out value))
				throw AppExcept (EXCEPT_BOOL, name, str_value);


			return value;
		}

		ushort? TryGetUInt16 (string name)
		{
			string str_value = GetString (name);

			if (str_value == null)
				return null;

			ushort value;
			if(!UInt16.TryParse (str_value, out value))
				throw AppExcept (EXCEPT_UINT16, name, str_value);

			return value;
		}

		string GetString (string name)
		{
			string str_value;
			if (!TryGetString (name, out str_value))
				throw AppExcept (EXCEPT_UNREGISTERED, name);
			return str_value;
		}

		static ApplicationException AppExcept (Exception except,
							string message,
							params object [] args)
		{
			return new ApplicationException (String.Format (
				CultureInfo.InvariantCulture, message, args),
				except);
		}
		
		static ApplicationException AppExcept (string message,
							params object [] args)
		{
			return new ApplicationException (String.Format (
				CultureInfo.InvariantCulture, message, args));
		}

		static NameValueCollection AppSettings {
			get { return System.Configuration.ConfigurationManager.AppSettings; }
		}
		
		static string GetXmlValue (XmlElement elem, string name)
		{
			string value = elem.GetAttribute (name);
			if (!String.IsNullOrEmpty(value))
				return value;
			
			foreach (XmlElement child in elem.GetElementsByTagName (name)) {
				value = child.InnerText;
				if (!String.IsNullOrEmpty(value))
					return value;
			}
			
			return String.Empty;
		}
	}
}
