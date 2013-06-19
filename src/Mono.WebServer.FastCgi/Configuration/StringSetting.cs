using System;

namespace Mono.WebServer.FastCgi.Configuration {
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