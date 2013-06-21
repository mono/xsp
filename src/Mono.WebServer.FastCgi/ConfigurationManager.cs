using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Mono.WebServer.FastCgi.Configuration;

namespace Mono.WebServer {
	[Obsolete]
	public class ConfigurationManager {
		readonly FastCgi.ConfigurationManager configurationManager = new FastCgi.ConfigurationManager ();

		public ConfigurationManager (Assembly asm, string resource)
		{
			var doc = new XmlDocument ();
			Stream stream = asm.GetManifestResourceStream (resource);
			if (stream != null)
				doc.Load (stream);
			configurationManager.ImportSettings (doc, false, SettingSource.Xml);
		}

		public bool Contains (string name)
		{
			return configurationManager.Contains (name);
		}

		[Obsolete]
		public object this [string name] {
			get {
				return configurationManager.GetSetting (name).ObjectValue;
			}
			
			set {
				configurationManager.SetValue (name, value);
			}
		}
	}
}
