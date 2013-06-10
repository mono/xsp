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
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections.Specialized;
using System.Text;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer
{
	public class ConfigurationManager
	{
		XmlNodeList settings;
		
		readonly IDictionary<string,string> cmd_args = new Dictionary<string,string> ();
		
		readonly IDictionary<string,string> xml_args = new Dictionary<string,string> ();

		readonly IDictionary<string,string> default_args = new Dictionary<string,string> ();
		
		public ConfigurationManager (Assembly asm, string resource)
		{
			var doc = new XmlDocument ();
			doc.Load (asm.GetManifestResourceStream (resource));
			ImportSettings (doc, default_args, false);
		}

		void ImportSettings (XmlDocument doc,
		                     IDictionary<string,string> collection,
		                     bool insertEmptyValue)
		{
			settings = doc.GetElementsByTagName ("Setting");
			foreach (XmlElement setting in settings) {
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
			}
		}
		
		XmlElement GetSetting (string name)
		{
			foreach (XmlElement setting in settings)
				if (GetXmlValue (setting, "Name") == name)
					return setting;
			
			return null;
		}

		string GetValue (string name, out XmlElement setting)
		{
			setting = GetSetting (name);
			
			if (setting == null)
				return null;
			
			string value;
			if (cmd_args.TryGetValue(name, out value))
				return value;
			if (xml_args.TryGetValue(name, out value))
				return value;

			string env_setting = GetXmlValue (setting, "Environment");
			if (env_setting.Length > 0){
				value = Environment.GetEnvironmentVariable(
					env_setting);
				if (value != null)
					return value;
			}
			
			string app_setting = GetXmlValue (setting,
				"AppSetting");

			if (app_setting.Length > 0)
				if ((value = AppSettings [app_setting]) != null)
					return value;

			default_args.TryGetValue(name, out value);
			return value;
		}
		
		public bool Contains (string name)
		{
			XmlElement setting;
			return GetValue (name, out setting) != null;
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
		
		public object this [string name] {
			get {
				XmlElement setting;
				string value = GetValue (name, out setting);
				
				if (setting == null)
					throw AppExcept (EXCEPT_UNREGISTERED,
						name);
				
				string type = GetXmlValue (setting,
					"Type").ToLower (
						CultureInfo.InvariantCulture);
				
				if (value == null)
					switch (type) {
					case "uint16":
						return default(UInt16);
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
							EXCEPT_UINT16,
							name, value);
					}
					
				case "bool":
					if (value.ToLower () == "true")
						return true;
					
					if (value.ToLower () == "false")
						return false;

					throw AppExcept (EXCEPT_BOOL, name,
						value);
					
				case "directory":
					if (Directory.Exists (value))
						return value;
					
					throw AppExcept (EXCEPT_DIRECTORY, name,
						value);
					
				case "file":
					if (File.Exists (value))
						return value;
					
					throw AppExcept (EXCEPT_FILE, name,
						value);
					
				default:
					throw AppExcept (EXCEPT_UNKNOWN, name,
						type);
				}
			}
			
			set {
				cmd_args.AddOrReplace (name, value.ToString ());
			}
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
		
		public void PrintHelp ()
		{
			int left_margin = 0;
			foreach (XmlElement setting in settings) {
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
			
			foreach (XmlElement setting in settings) {
				string show = GetXmlValue (setting,
					"ConsoleVisible").ToLower (
						CultureInfo.InvariantCulture);
				
				if (show != "true")
					continue;
				
				string type = GetXmlValue (setting,
					"Type").ToUpper (
						CultureInfo.InvariantCulture);

				string name = GetXmlValue (setting, "Name"); 
				string arg = String.Format (
					CultureInfo.InvariantCulture,
					"  /{0}{1}",
					name,
					type == "BOOL" ? "[=True|=False]" :
						"=" + type);
				
				Console.Write (arg);
				
				var values = new List<string> ();
				foreach (XmlElement desc in setting.GetElementsByTagName ("Description"))
					RenderXml (desc, values, 0, 78 - left_margin);
				
				string app_setting = GetXmlValue (setting,
					"AppSetting");
				
				if (app_setting.Length > 0) {
					string val = AppSettings [app_setting];
					
					if (String.IsNullOrEmpty (val))
						default_args.TryGetValue (name, out val);

					if (String.IsNullOrEmpty (val))
						val = "none";
					
					values.Add (" Default Value: " + val);
					
					values.Add (" AppSettings Key Name: " +
						app_setting);
					
				}

				string env_setting = GetXmlValue (setting,
					"Environment");
				
				if (env_setting.Length > 0)
					values.Add (" Environment Variable Name: " +
						env_setting);
				
				values.Add (String.Empty);

				int start = arg.Length;
				foreach (string text in values) {
					for (int i = start; i < left_margin; i++)
						Console.Write (' ');
					start = 0;
					Console.WriteLine (text);
				}
			}
		}
		
		void RenderXml (XmlElement elem, List<string> values, int indent, int length)
		{
			foreach (XmlNode node in elem.ChildNodes) {
				var child = node as XmlElement;
				switch (node.LocalName) {
					case "para":
						RenderXml (child, values, indent, length);
						values.Add (String.Empty);
						break;
					case "block":
						RenderXml (child, values, indent + 4, length);
						break;
					case "example":
						RenderXml (child, values, indent + 4, length);
						values.Add (String.Empty);
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
		
		void RenderText (string text, List<string> values, int indent, int length)
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
		
		StringBuilder CreateBuilder (int indent)
		{
			var builder = new StringBuilder (80);
			for (int i = 0; i < indent; i ++)
				builder.Append (' ');
			return builder;
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

				cmd_args.AddOrReplace (arg, value);
			}
		}
		
		const string EXCEPT_BAD_ELEM =
			"XML setting \"{0}={1}\" is invalid.";
		
		const string EXCEPT_XML_DUPLICATE =
			"XML setting \"{0}\" can only be assigned once.";
		
		public void LoadXmlConfig (string filename)
		{
			var doc = new XmlDocument ();
			doc.Load (filename);
			ImportSettings (doc, xml_args, true);
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
