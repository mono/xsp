using System;
using System.Collections.Generic;
using System.Xml;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer {
	public partial class ConfigurationManager {
		const string EXCEPT_BAD_ELEM =
			"XML setting \"{0}={1}\" is invalid.";

		const string EXCEPT_XML_DUPLICATE =
			"XML setting \"{0}\" can only be assigned once.";

		void ImportSettings (XmlDocument doc,
		                     IDictionary<string,string> collection,
		                     bool insertEmptyValue, bool loadInfo)
		{
			var tags = doc.GetElementsByTagName ("Setting");
			foreach (XmlElement setting in tags) {
				string name = GetXmlValue (setting, "Name");
				string value = GetXmlValue (setting, "Value");
				if (name.Length == 0)
					throw AppExcept (EXCEPT_BAD_ELEM,
					                 name, value);

				if (collection.ContainsKey (name))
					throw AppExcept (EXCEPT_XML_DUPLICATE,
					                 name);

				if (insertEmptyValue || value.Length > 0)
					collection.Add (name, value);

				if (loadInfo && !settingsInfo.Contains (name)) {
					string consoleVisible = GetXmlValue (setting, "ConsoleVisible");
					string type = GetXmlValue (setting, "Type");
					string environment = GetXmlValue (setting, "Environment");
					string appSetting = GetXmlValue (setting, "AppSetting");
					XmlNodeList description = setting.GetElementsByTagName ("Description");
					settingsInfo.Add (new SettingInfo (consoleVisible, type, name,
					                               environment, appSetting, description, value));
				}
			}
			if (!loadInfo)
				return;
			maxConnsDefault = UInt16.Parse(settingsInfo ["maxconns"].Value);
			maxReqsDefault = UInt16.Parse (settingsInfo ["maxreqs"].Value);
			socketDefault = settingsInfo ["socket"].Value;
			portDefault = UInt16.Parse (settingsInfo ["port"].Value);
		}

		public void LoadCommandLineArgs (string [] args)
		{
			if (args == null)
				throw new ArgumentNullException ("args");

			for (int i = 0; i < args.Length; i ++) {
				string arg = args [i];
				int len = PrefixLength (arg);
				
				if (len > 0)
					arg = arg.Substring (len);
				else {
					Console.WriteLine (
						"Warning: \"{0}\" is not a valid argument. Ignoring.",
						args [i]);
					continue;
				}
				
				if (cmd_args.ContainsKey (arg))
					Console.WriteLine (
						"Warning: \"{0}\" has already been set. Overwriting.",
						args [i]);
				
				string [] pair = arg.Split (new[] {'='}, 2);
				
				if (pair.Length == 2) {
					cmd_args.AddOrReplace (pair [0],
					                       pair [1]);
					continue;
				}
				
				SettingInfo setting = GetSettingInfo (arg);
				if (setting == null) {
					Console.WriteLine (
						"Warning: \"{0}\" is an unknown argument. Ignoring.",
						args [i]);
					continue;
				}

				SettingInfo.SettingType type = setting.Type;
				string value;

				if (type == SettingInfo.SettingType.Bool) {
					bool parsed;
					if (i + 1 < args.Length && Boolean.TryParse (args [i + 1], out parsed)) {
						value = parsed.ToString ();
						i++;
					} else
						value = "True";
				} else if (i + 1 < args.Length)
					value = args [++i];
				else {
					Console.WriteLine (
						"Warning: \"{0}\" is missing its value. Ignoring.",
						args [i]);
					continue;
				}

				cmd_args.AddOrReplace (arg, value);
			}
		}

		int PrefixLength (string arg)
		{
			if (arg.StartsWith ("--"))
				return 2;
			
			if (arg.StartsWith ("-"))
				return 1;
			
			if (arg.StartsWith ("/"))
				return 1;
			
			return 0;
		}

		public void LoadXmlConfig (string filename)
		{
			var doc = new XmlDocument ();
			doc.Load (filename);
			ImportSettings (doc, xml_args, true, false);
		}
	}
}
