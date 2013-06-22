using System;

namespace Mono.WebServer.Options {
	public class BoolSetting : Setting<bool>
	{
		public BoolSetting (string name, string description, string appSetting = null, string environment = null, bool defaultValue = false, string prototype = null)
			: base (name, Boolean.TryParse, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}