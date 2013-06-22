using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Mono.WebServer.Options;
using NDesk.Options;

namespace Mono.WebServer.FastCgi {
	public partial class ConfigurationManager
	{
		const string EXCEPT_BAD_ELEM = "XML setting \"{0}={1}\" is invalid.";

		const string EXCEPT_XML_DUPLICATE = "XML setting \"{0}\" can only be assigned once.";

		static ApplicationException AppExcept (string message, params object [] args)
		{
			return new ApplicationException (String.Format (CultureInfo.InvariantCulture, message, args));
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


		internal void ImportSettings (XmlDocument doc, bool insertEmptyValue, SettingSource source)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			var tags = doc.GetElementsByTagName ("Setting");
			foreach (XmlElement setting in tags) {
				string name = GetXmlValue (setting, "Name");
				string value = GetXmlValue (setting, "Value");
				if (name.Length == 0)
					throw AppExcept (EXCEPT_BAD_ELEM, name, value);

				if (Settings.Contains (name))
					throw AppExcept (EXCEPT_XML_DUPLICATE, name);

				if (insertEmptyValue || value.Length > 0)
					Settings[name].MaybeParseUpdate (source, value);
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
			foreach (ISetting setting in Settings) {
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
			if (String.IsNullOrEmpty (file))
				throw new ArgumentNullException ("file");
			var doc = new XmlDocument ();
			doc.Load (file);
			ImportSettings (doc, true, SettingSource.Xml);
		}
	}
}
