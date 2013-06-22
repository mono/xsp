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
using System.Xml;
using System.Globalization;
using Mono.WebServer.Options;

namespace Mono.WebServer.FastCgi {
	public partial class ConfigurationManager
	{
		internal static ApplicationException AppExcept (string message,
							params object [] args)
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

		[Obsolete]
		internal void SetValue (string name, object value)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			settings[name].MaybeParseUpdate (SettingSource.CommandLine, value.ToString ());
		}

		[Obsolete]
		internal ISetting GetSetting (string name)
		{
			return settings [name];
		}

		public void PrintHelp ()
		{
			CreateOptionSet ().WriteOptionDescriptions (Console.Out);
		}

		public bool Contains (string name)
		{
			return settings.Contains (name);
		}
	}
}
