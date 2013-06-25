using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using NDesk.Options;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager 
	{
		const string EXCEPT_BAD_ELEM = "XML setting \"{0}={1}\" is invalid.";

		const string EXCEPT_XML_DUPLICATE = "XML setting \"{0}\" can only be assigned once.";

		protected abstract string Name { get; }
		protected abstract string Description { get; }

		[Obsolete("Not to be used by external classes, will be private")]
		public void ImportSettings (XmlDocument doc, bool insertEmptyValue, SettingSource source)
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

		protected OptionSet CreateOptionSet ()
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
#pragma warning disable 612,618
			ImportSettings (doc, true, SettingSource.Xml);
#pragma warning restore 612,618
		}

		static ApplicationException AppExcept (string message, params object [] args)
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

		public bool LoadCommandLineArgs (string [] cmd_args)
		{
			if (cmd_args == null)
				throw new ArgumentNullException ("cmd_args");

			OptionSet optionSet = CreateOptionSet ();

			List<string> extra;
			try {
				extra = optionSet.Parse (cmd_args);
			} catch (OptionException e) {
				Console.WriteLine ("{0}: {1}", Name, e.Message);
				Console.WriteLine ("Try `{0} --help' for more information.", Name);
				return false;
			}

			if (extra.Count > 0) {
				Console.Write("Warning: unparsed command line arguments: ");
				foreach (string s in extra) {
					Console.Write ("{0} ", s);
				}
				Console.WriteLine();
			}

			return true;
		}
	}
}
