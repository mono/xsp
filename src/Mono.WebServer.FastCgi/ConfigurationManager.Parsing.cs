using System;
using System.Collections.Generic;
using System.Xml;
using Mono.WebServer.FastCgi.Configuration;
using NDesk.Options;

namespace Mono.WebServer.FastCgi {
	public partial class ConfigurationManager {
		const string EXCEPT_BAD_ELEM =
			"XML setting \"{0}={1}\" is invalid.";

		const string EXCEPT_XML_DUPLICATE =
			"XML setting \"{0}\" can only be assigned once.";

		internal void ImportSettings (XmlDocument doc, bool insertEmptyValue,
			SettingSource source)
		{
			var tags = doc.GetElementsByTagName ("Setting");
			foreach (XmlElement setting in tags) {
				string name = GetXmlValue (setting, "Name");
				string value = GetXmlValue (setting, "Value");
				if (name.Length == 0)
					throw AppExcept (EXCEPT_BAD_ELEM, name, value);

				if (settings.Contains (name))
					throw AppExcept (EXCEPT_XML_DUPLICATE, name);

				if (insertEmptyValue || value.Length > 0)
					settings[name].MaybeParseUpdate (source, value);
			}
		}

		public void LoadCommandLineArgs (string [] cmd_args)
		{
			if (cmd_args == null)
				throw new ArgumentNullException ("cmd_args");

			OptionSet optionSet = CreateOptionSet ();

			List<string> extra;
			try {
				extra = optionSet.Parse (cmd_args);
			} catch (OptionException e) {
				Console.Write ("mono-fastcgi: ");
				Console.WriteLine (e.Message);
				Console.WriteLine ("Try `greet --help' for more information.");
				return;
			}

			if (extra.Count > 0) {
				Console.Write("Unparsed command line arguments: ");
				foreach (string s in extra) {
					Console.Write ("{0} ", s);
				}
				Console.WriteLine();
			}
		}

		OptionSet CreateOptionSet ()
		{
			var p = new OptionSet ();
			foreach (ISetting setting in settings) {
				var boolSetting = setting as Setting<bool>;
				if (boolSetting != null) {
					p.Add (setting.Prototype, setting.Description,
					       v => boolSetting.MaybeUpdate (SettingSource.CommandLine, v != null));
				} else {
					ISetting setting1 = setting; // Used in closure, must copy
					p.Add (setting.Prototype, setting.Description,
					       v =>
					       { if (v != null) setting1.MaybeParseUpdate (SettingSource.CommandLine, v);
					       });
				}
			}
			return p;
		}

		public void LoadXmlConfig (string file)
		{
			var doc = new XmlDocument ();
			doc.Load (file);
			ImportSettings (doc, true, SettingSource.Xml);
		}
	}
}
