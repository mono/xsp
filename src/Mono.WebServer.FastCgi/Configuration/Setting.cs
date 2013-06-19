using System;

namespace Mono.WebServer.FastCgi.Configuration {
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
			Value = new Tuple<SettingSource, T> (SettingSource.Unset, defaultValue);
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
			if (source < Value.Item1)
				return false;
			Value = new Tuple<SettingSource, T> (source, value);
			return true;
		}

		public static implicit operator T (Setting<T> setting)
		{
			return setting.Value.Item2;
		}

		public bool ConsoleVisible { get; private set; }
		public string Name { get; private set; }
		public string Prototype { get; private set; }
		public string Environment { get; private set; }
		public string AppSetting { get; private set; }
		public string Description { get; private set; }
		public T DefaultValue { get; private set; }
		public Tuple<SettingSource, T> Value { get; private set; }
		public object ObjectValue { get { return Value.Item2; } }
	}
}