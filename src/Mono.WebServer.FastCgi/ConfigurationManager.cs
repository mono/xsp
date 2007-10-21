using System;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace Mono.WebServer
{
	public class ConfigurationManager
	{
		private int hash;
		
		private XmlNodeList elems;
		
		private NameValueCollection cmd_args = 
			new NameValueCollection ();
		
		private NameValueCollection xml_args =
			new NameValueCollection ();
		
		public ConfigurationManager (Assembly asm, string resource)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (asm.GetManifestResourceStream (resource));
			elems = doc.GetElementsByTagName ("Setting");
		}
		
		private XmlElement GetSetting (string name)
		{
			foreach (XmlElement setting in elems)
				if (GetXmlValue (setting, "Name") == name)
					return setting;
			
			return null;
		}
		
		private string GetValue (string name, out XmlElement setting)
		{
			setting = GetSetting (name);
			
			if (setting == null)
				return null;
			
			string value;
			if ((value = cmd_args [name]) != null) return value;
			if ((value = xml_args [name]) != null) return value;
			
			string app_setting = GetXmlValue (setting,
				"AppSetting");
			
			return app_setting.Length > 0 ?
				AppSettings [app_setting] : null;
		}
		
		public bool Contains (string name)
		{
			XmlElement setting;
			return GetValue (name, out setting) != null;
		}
		
		private static string except_unregistered =
			"Argument \"{0}\" is unknown.";
		
		private static string except_uint16 =
			"Error in argument \"{0}\". \"{1}\" cannot be converted to an integer.";
		
		private static string except_bool =
			"Error in argument \"{0}\". \"{1}\" should be \"True\" or \"False\".";
		
		private static string except_directory =
			"Error in argument \"{0}\". \"{1}\" is not a directory or does not exist.";
			
		private static string except_file =
			"Error in argument \"{0}\". \"{1}\" does not exist.";
		
		private static string except_unknown =
			"The Argument \"{0}\" has an invalid type: {1}.";
		
		public object this [string name] {
			get {
				XmlElement setting;
				string value = GetValue (name, out setting);
				
				if (setting == null)
					throw AppExcept (except_unregistered,
						name);
				
				string type = GetXmlValue (setting,
					"Type").ToLower (
						CultureInfo.InvariantCulture);
				
				if (value == null)
					switch (type) {
					case "uint16":
						return 0;
					case "bool":
						return false;
					default:
						return null;
					}
				
				switch (type) {
				case "string":
					return value;
					
				case "uint16":
					try {
						return ushort.Parse (value);
					} catch (Exception except) {
						throw AppExcept (except,
							except_uint16,
							name, value);
					}
					
				case "bool":
					if (value.ToLower () == "true")
						return true;
					
					if (value.ToLower () == "false")
						return false;
						
					throw AppExcept (except_bool, name,
						value);
					
				case "directory":
					DirectoryInfo dir = new DirectoryInfo (
						value);
					if (dir.Exists)
						return value;
					
					throw AppExcept (except_directory, name,
						value);
					
				case "file":
					FileInfo file = new FileInfo (value);
					if (file.Exists)
						return value;
					
					throw AppExcept (except_file, name,
						value);
					
				default:
					throw AppExcept (except_unknown, name,
						type);
				}
			}
			
			set {
				cmd_args.Set (name, value.ToString ());
			}
		}
		
		private ApplicationException AppExcept (Exception except,
		                                        string message,
		                                        params object [] args)
		{
			return new ApplicationException (string.Format (
				CultureInfo.InvariantCulture, message, args),
				except);
		}
		
		private ApplicationException AppExcept (string message,
		                                        params object [] args)
		{
			return new ApplicationException (string.Format (
				CultureInfo.InvariantCulture, message, args));
		}
		
		public void PrintHelp ()
		{
			int left_margin = 0;
			foreach (XmlElement setting in elems) {
				string show = GetXmlValue (setting,
					"ConsoleVisible").ToLower (
						CultureInfo.InvariantCulture);
				
				if (show != "true")
					continue;
				
				string type = GetXmlValue (setting,
					"Type").ToUpper (
						CultureInfo.InvariantCulture);
				
				int length = 4 +
					GetXmlValue (setting, "Name").Length +
					(type == "BOOL" ? 14 : type.Length + 1);
				
				if (length > left_margin)
					left_margin = length;
			}
			
			foreach (XmlElement setting in elems) {
				string show = GetXmlValue (setting,
					"ConsoleVisible").ToLower (
						CultureInfo.InvariantCulture);
				
				if (show != "true")
					continue;
				
				string type = GetXmlValue (setting,
					"Type").ToUpper (
						CultureInfo.InvariantCulture);
				
				string arg = string.Format (
					CultureInfo.InvariantCulture,
					"  /{0}{1}",
					GetXmlValue (setting, "Name"),
					type == "BOOL" ? "[=True|=False]" :
						"=" + type);
				
				Console.Write (arg);
				
				ArrayList values = new ArrayList ();
				foreach (XmlElement desc in setting.GetElementsByTagName ("Description"))
					RenderXml (desc, values, 0, 78 - left_margin);
				
				string app_setting = GetXmlValue (setting,
					"AppSetting");
				
				if (app_setting.Length > 0) {
					string val = AppSettings [app_setting];
					
					if (val == null || val.Length == 0)
						val = "none";
					
					values.Add (" Default Value: " + val);
					
					values.Add (" AppSettings Key Name: " +
						app_setting);
					
					values.Add (string.Empty);
				}
				
				int start = arg.Length;
				foreach (string text in values) {
					for (int i = start; i < left_margin; i++)
						Console.Write (' ');
					start = 0;
					Console.WriteLine (text);
				}
			}
		}
		
		private void RenderXml (XmlElement elem, ArrayList values, int indent, int length)
		{
			foreach (XmlNode node in elem.ChildNodes) {
				XmlElement child = node as XmlElement;
				switch (node.LocalName) {
					case "para":
						RenderXml (child, values, indent, length);
						values.Add (string.Empty);
						break;
					case "block":
						RenderXml (child, values, indent + 4, length);
						break;
					case "example":
						RenderXml (child, values, indent + 4, length);
						values.Add (string.Empty);
						break;
					case "code":
					case "desc":
						RenderXml (child, values, indent, length);
						break;
					case "#text":
						RenderText (node.Value, values, indent, length);
						break;
				}
			}
		}
		
		private void RenderText (string text, ArrayList values, int indent, int length)
		{
			StringBuilder output = CreateBuilder (indent);
			int start = -1;
			for (int i = 0; i <= text.Length; i ++) {
				bool ws = i == text.Length || char.IsWhiteSpace (text [i]);
				
				if (ws && start >= 0) {
					if (output.Length + i - start > length) {
						values.Add (output.ToString ());
						output = CreateBuilder (indent);
					}
					
					output.Append (' ');
					output.Append (text.Substring (start, i - start));
					
					start = -1;
				} else if (!ws && start < 0) {
					start = i;
				}
			}
			values.Add (output.ToString ());
		}
		
		private StringBuilder CreateBuilder (int indent)
		{
			StringBuilder builder = new StringBuilder (80);
			for (int i = 0; i < indent; i ++)
				builder.Append (' ');
			return builder;
		}
		
		public void LoadCommandLineArgs (string [] args)
		{
			if (args == null)
				throw new ArgumentNullException ("args");
			
			for (int i = 0; i < args.Length; i ++) {
				// Randomize the hash a bit.
				int idx = (i + 1 < args.Length) ? i + 1 : i;
				hash ^= args [idx].GetHashCode () + i;
				
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
				
				if (cmd_args [arg] != null)
					Console.WriteLine (
						"Warning: \"{0}\" has already been set. Overwriting.",
						args [i]);
				
				string [] pair = arg.Split (new char [] {'='},
					2);
				
				if (pair.Length == 2) {
					cmd_args.Add (pair [0], pair [1]);
					continue;
				}
				
				XmlElement setting = GetSetting (arg);
				if (setting == null) {
					Console.WriteLine (
						"Warning: \"{0}\" is an unknown argument. Ignoring.",
						args [i]);
					continue;
				}
				
				string type = GetXmlValue (setting,
					"Type").ToLower (
						CultureInfo.InvariantCulture);
				string value;
				
				if (type == "bool")
					value = (i + 1 < args.Length &&
						(args [i+1].ToLower () == "true" ||
						args [i+1].ToLower () == "false")) ?
						args [++ i] : "True";
				else if (i + 1 < args.Length)
					value = args [++i];
				else {
					Console.WriteLine (
						"Warning: \"{0}\" is missing its value. Ignoring.",
						args [i]);
					continue;
				}
				
				cmd_args [arg] = value;
			}
		}
		
		private static readonly string except_bad_elem =
			"XML setting \"{0}={1}\" is invalid.";
		
		private static readonly string except_xml_duplicate =
			"XML setting \"{0}\" can only be assigned once.";
		
		public void LoadXmlConfig (string filename)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (filename);
			foreach (XmlElement setting in
				doc.GetElementsByTagName ("Setting")) {
				string name = GetXmlValue (setting, "Name");
				string value = GetXmlValue (setting, "Value");
				if (name.Length == 0)
					throw AppExcept (except_bad_elem,
						name, value);
					
				if (xml_args [name] != null)
					throw AppExcept (except_xml_duplicate,
						name);
					
				xml_args.Add (name, value);
			}
		}
		
		private int PrefixLength (string arg)
		{
			if (arg.StartsWith ("--"))
				return 2;
			
			if (arg.StartsWith ("-"))
				return 1;
			
			if (arg.StartsWith ("/"))
				return 1;
			
			return 0;
		}
		
		public int Hash {
			get {return hash < 0 ? -hash : hash;}
		}
		
		private static NameValueCollection AppSettings {
			get {
				#if NET_2_0
				return System.Configuration.ConfigurationManager.AppSettings;
				#else
				return System.Configuration.ConfigurationSettings.AppSettings;
				#endif
			}
		}
		
		private static string GetXmlValue (XmlElement elem, string name)
		{
			string value = elem.GetAttribute (name);
			if (value != null && value.Length != 0)
				return value;
			
			foreach (XmlElement child in elem.GetElementsByTagName (name)) {
				value = child.InnerText;
				if (value != null && value.Length != 0)
					return value;
			}
			
			return string.Empty;
		}
	}
}
