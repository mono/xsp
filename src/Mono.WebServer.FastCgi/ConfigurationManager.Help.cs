using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer {
	public partial class ConfigurationManager {
		public void PrintHelp ()
		{
			int left_margin = 0;
			foreach (var setting in settingsInfo) {
				bool show = setting.ConsoleVisible;
				
				if (!show)
					continue;

				SettingInfo.SettingType type = setting.Type;

				int typelength = type == SettingInfo.SettingType.Bool ? 14 : type.ToString ().Length + 1;
				int length = 4 + setting.Name.Length + typelength;
				
				if (length > left_margin)
					left_margin = length;
			}
			
			foreach (var setting in settingsInfo) {
				bool show = setting.ConsoleVisible;
				
				if (!show)
					continue;

				SettingInfo.SettingType type = setting.Type;

				string name = setting.Name;
				string arg = String.Format (
					CultureInfo.InvariantCulture,
					"  /{0}{1}",
					name,
					type == SettingInfo.SettingType.Bool ? "[=True|=False]" :
						"=" + type);
				
				Console.Write (arg);
				
				var values = new List<string> ();
				foreach (XmlElement desc in setting.Description)
					RenderXml (desc, values, 0, 78 - left_margin);
				
				string app_setting = setting.AppSetting;
				
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

				string env_setting = setting.Environment;
				
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

		void RenderXml (XmlElement elem, ICollection<string> values, int indent, int length)
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

		void RenderText (string text, ICollection<string> values, int indent, int length)
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
	}
}
