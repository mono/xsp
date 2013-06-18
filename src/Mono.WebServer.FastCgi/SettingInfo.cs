using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Mono.WebServer.FastCgi {
	class SettingInfo {
		public enum SettingType {
			Bool,
			UInt16,
			String,
			Directory,
			File
		}

		public SettingInfo (string consoleVisible, string type, string name,
		                    string environment, string appSetting, XmlNodeList description)
		{
			Description = description.Cast<XmlElement>();
			AppSetting = appSetting;
			Environment = environment;
			Name = name;
#if NET_4_0
			SettingType parsed;
			if (Enum.TryParse(type, true, out parsed)) {
				Type = parsed;
			} else {
#else
			try {
				Type = (SettingType)Enum.Parse (typeof (SettingType), type, true);
			} catch {
#endif
				throw new ArgumentException("Couldn't parse " + type + " as a type.");
			}
			bool visible;
			if(!Boolean.TryParse (consoleVisible, out visible))
				throw new ArgumentException ("Couldn't parse " + consoleVisible + " as a boolean.");
			ConsoleVisible = visible;
		}

		public bool ConsoleVisible { get; private set; }
		public SettingType Type { get; private set; }
		public string Name { get; private set; }
		public string Environment { get; private set; }
		public string AppSetting { get; private set; }
		public IEnumerable<XmlElement> Description { get; private set; }
	}
}