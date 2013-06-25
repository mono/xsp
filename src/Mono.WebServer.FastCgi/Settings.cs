//
// Settings.cs.
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialene@gmail.com>
//
// Copyright (C) 2013 Leonardo Taglialegne
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

namespace Mono.WebServer.FastCgi {
	enum SettingSource {
		Default,
		AppSettings,
		Environment,
		Xml,
		CommandLine
	}

	interface ISetting {
		string Name { get; }
		bool ConsoleVisible { get; }
		string Description { get; }
		string AppSetting { get; }
		string Environment { get; }
		string Prototype { get; }
		[Obsolete]
		object Value { get; }

		void MaybeParseUpdate (SettingSource settingSource, string value);
	}

	delegate bool Parser<T> (string input, out T output);

	class Setting<T> : ISetting {
		// TODO: Find a clean way to make this static
		readonly Parser<T> parser;

		public Setting (string name, Parser<T> parser, string description, string appSetting = null,
			string environment = null, T defaultValue = default (T), bool consoleVisible = true, string prototype = null)
		{
			Prototype = prototype ?? name;
			Description = description;
			AppSetting = appSetting;
			Environment = environment;
			Name = name;
			ConsoleVisible = consoleVisible;
			DefaultValue = defaultValue;
			this.parser = parser;
			Value = defaultValue;
			if (!String.IsNullOrEmpty (Environment)) {
				string value = System.Environment.GetEnvironmentVariable (Environment);
				MaybeParseUpdate (SettingSource.Environment, value);
			}

			if (!String.IsNullOrEmpty (AppSetting)) {
				string value = System.Configuration.ConfigurationManager.AppSettings [AppSetting];
				MaybeParseUpdate (SettingSource.AppSettings, value);
			}
		}

		public void MaybeParseUpdate (SettingSource settingSource, string value)
		{
			if (value == null)
				return;
			T result;
			if (parser (value, out result))
				MaybeUpdate (settingSource, result);
		}

		public bool MaybeUpdate (SettingSource source, T value)
		{
			if (source < this.source)
				return false;
			Value = value;
			this.source = source;
			return true;
		}

		public static implicit operator T (Setting<T> setting)
		{
			return setting.Value;
		}

		SettingSource source = SettingSource.Default;
		public bool ConsoleVisible { get; private set; }
		public string Name { get; private set; }
		public string Prototype { get; private set; }
		public string Environment { get; private set; }
		public string AppSetting { get; private set; }
		public string Description { get; private set; }
		public T DefaultValue { get; private set; }
		public T Value { get; private set; }
		object ISetting.Value { get { return Value; } }
	}

	class BoolSetting : Setting<bool> {
		public BoolSetting (string name, string description, string appSetting = null, string environment = null, bool defaultValue = false, bool consoleVisible = true, string prototype = null)
			: base (name, Boolean.TryParse, description, appSetting, environment, defaultValue, consoleVisible, prototype)
		{
		}
	}

	class UInt16Setting : Setting<ushort> {
		public UInt16Setting (string name, string description, string appSetting = null, string environment = null, ushort defaultValue = default(UInt16), bool consoleVisible = true, string prototype = null)
			: base (name, UInt16.TryParse, description, appSetting, environment, defaultValue, consoleVisible, prototype)
		{
		}
	}

	class StringSetting : Setting<string> {
		public StringSetting (string name, string description, string appSetting = null, string environment = null, string defaultValue = null, bool consoleVisible = true, string prototype = null)
			: base (name, FakeParse, description, appSetting, environment, defaultValue, consoleVisible, prototype)
		{
		}

		static bool FakeParse (string value, out string result)
		{
			result = value;
			return !String.IsNullOrEmpty (value);
		}
	}
}