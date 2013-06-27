using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Mono.Unix;
using Mono.WebServer.Log;
using NDesk.Options;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		const string EXCEPT_BAD_ELEM = "XML setting \"{0}={1}\" is invalid.";

		protected abstract string Name { get; }
		protected abstract string Description { get; }

		[Obsolete]
		protected SettingsCollection Settings {
			get { return settings; }
		}

		readonly SettingsCollection settings;

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
				Console.Write ("Warning: unparsed command line arguments: ");
				foreach (string s in extra) {
					Console.Write ("{0} ", s);
				}
				Console.WriteLine ();
			}

			return true;
		}

		public void LoadXmlConfig (string file)
		{
			if (String.IsNullOrEmpty (file))
				throw new ArgumentNullException ("file");
			var fileInfo = new UnixFileInfo (file);
			var doc = new XmlDocument ();
			doc.Load (file);
			ImportSettings (doc, true, fileInfo);
		}
		
		[Obsolete("Not to be used by external classes, will be private")]
		public void ImportSettings (XmlDocument doc, bool insertEmptyValue, UnixFileInfo file)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");

			var tags = doc.GetElementsByTagName ("Setting");
			foreach (XmlElement setting in tags) {
				string name = GetXmlValue (setting, "Name");
				string value = Parse (GetXmlValue (setting, "Value"), file);
				if (name.Length == 0)
					throw AppExcept (EXCEPT_BAD_ELEM, name, value);

				if (settings.Contains (name)) {
					if (insertEmptyValue || value.Length > 0)
						settings [name].MaybeParseUpdate (SettingSource.Xml, value);
				} else
					Logger.Write (LogLevel.Warning, "Unrecognized xml setting: {0} with value {1}", name, value);
			}
		}

		string Parse (string value, UnixFileInfo file)
		{
			if (file == null)
				return value;
			return value.Replace ("$(user)", file.OwnerUser.UserName)
			            .Replace ("$(group)", file.OwnerGroup.GroupName)
			            .Replace ("$(filename)", Path.GetFileNameWithoutExtension(file.Name));
		}

		protected OptionSet CreateOptionSet ()
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
					       v => { if (v != null) setting1.MaybeParseUpdate (SettingSource.CommandLine, v); } );
				}
			}
			return p;
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
	}
}
