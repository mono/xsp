using System;

namespace Mono.WebServer.Options {
	public class NullableInt32Setting : NullableSetting<int>
	{
		public NullableInt32Setting (string name, string description, string appSetting = null, string environment = null, int? defaultValue = null, string prototype = null)
			: base (name, Int32.TryParse, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}