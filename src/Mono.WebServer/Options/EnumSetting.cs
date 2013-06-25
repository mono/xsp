using System;

namespace Mono.WebServer.Options {
	public class EnumSetting<T> : Setting<T> where T : struct 
	{
#if !NET_4_0
		static bool EnumParser<TEnum> (string input, out TEnum output)
		{
			output = default (TEnum);
			try {
				output = (TEnum)Enum.Parse (typeof (TEnum), input);
				return true;
			} catch (ArgumentException) { // TODO: catch more specific type
				return false;
			}
		}
#endif

		public EnumSetting (string name, string description, string appSetting = null, string environment = null, T defaultValue = default(T), string prototype = null)
#if NET_4_0
			: base (name, Enum.TryParse, description, appSetting, environment, defaultValue, prototype)
#else
			: base (name, EnumParser, description, appSetting, environment, defaultValue, prototype)
#endif
		{
		}
	}
}
