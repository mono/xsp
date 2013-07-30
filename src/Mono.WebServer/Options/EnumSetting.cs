using System;

namespace Mono.WebServer.Options {
	public class EnumSetting<T> : Setting<T> where T : struct 
	{
		static bool EnumParser<TEnum> (string input, out TEnum output)
		{
			output = default (TEnum);
			try {
				output = (TEnum)Enum.Parse (typeof (TEnum), input, true);
				return true;
			} catch (ArgumentException) { // TODO: catch more specific type
				return false;
			}
		}

		public EnumSetting (string name, string description, string appSetting = null, string environment = null, T defaultValue = default(T), string prototype = null)
			: base (name, EnumParser, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}
