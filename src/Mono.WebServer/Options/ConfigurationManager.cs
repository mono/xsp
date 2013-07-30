using System;
using System.Globalization;
using System.IO;
using System.Xml;
using Mono.Unix;
using Mono.WebServer.Log;
using NDesk.Options;

namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager {
		const string EXCEPT_BAD_ELEM = "XML setting \"{0}={1}\" is invalid.";

		readonly SettingsCollection settings;

		[Obsolete]
		protected SettingsCollection Settings {
			get { return settings; }
		}


		public bool TryLoadXmlConfig (string file)
		{
			if (String.IsNullOrEmpty (file))
				throw new ArgumentNullException ("file");
			var doc = new XmlDocument ();
			try {
				doc.Load (file);
			} catch (FileNotFoundException e) {
				Console.Error.WriteLine("ERROR: Couldn't find configuration file {0}!", e.FileName);
				return false;
			}
			if (Platform.IsUnix) {
				var fileInfo = new UnixFileInfo (file);
				ImportSettings (doc, true, file, fileInfo.OwnerUser.UserName, fileInfo.OwnerGroup.GroupName);
			} else
				ImportSettings (doc, true, file);
			return true;
		}
		
		[Obsolete]
		public void ImportSettings (XmlDocument doc, bool insertEmptyValue)
		{
			ImportSettings (doc, insertEmptyValue, null);
		}

		void ImportSettings (XmlDocument doc, bool insertEmptyValue, string filename , string user = null, string group = null)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");

			var tags = doc.GetElementsByTagName ("Setting");
			foreach (XmlElement setting in tags) {
				string name = GetXmlValue (setting, "Name");
				string value = Parse (GetXmlValue (setting, "Value"), filename, user, group);
				if (name.Length == 0)
					throw AppExcept (EXCEPT_BAD_ELEM, name, value);

				if (settings.Contains (name)) {
					if (insertEmptyValue || value.Length > 0)
						settings [name].MaybeParseUpdate (SettingSource.Xml, value);
				} else
					Logger.Write (LogLevel.Warning, "Unrecognized xml setting: {0} with value {1}", name, value);
			}
		}

		static string Parse (string value, string filename, string user, string group)
		{
			if (!String.IsNullOrEmpty (filename))
				value = value.Replace ("$(filename)", Path.GetFileNameWithoutExtension (filename));
			if (!String.IsNullOrEmpty (user))
				value = value.Replace ("$(user)", user);
			if (!String.IsNullOrEmpty (group))
				value = value.Replace ("$(group)", group);
			return value;
		}

		public OptionSet CreateOptionSet ()
		{
			var p = new OptionSet ();
			foreach (ISetting setting in settings) {
				var boolSetting = setting as Setting<bool>;
				if (boolSetting != null) {
					p.Add (setting.Prototype, setting.Description,
					       v => boolSetting.MaybeUpdate (SettingSource.CommandLine, v != null));
				} else {
					ISetting setting1 = setting; // Used in closure, must copy
					p.Add (setting.Prototype + "=", setting.Description,
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
