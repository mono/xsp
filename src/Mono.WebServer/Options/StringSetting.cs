using System;

namespace Mono.WebServer.Options {
	public class StringSetting : Setting<string>
	{
		public StringSetting (string name, string description, string appSetting = null, string environment = null, string defaultValue = null, string prototype = null)
			: base (name, FakeParse, description, appSetting, environment, defaultValue, prototype)
		{
		}

		static bool FakeParse (string value, out string result)
		{
			result = value;
			return !String.IsNullOrEmpty (value);
		}
	}
}