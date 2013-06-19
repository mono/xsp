using System;

namespace Mono.WebServer.FastCgi.Configuration {
	class BoolSetting : Setting<bool> {
		public BoolSetting (string name, string description, string appSetting = null, string environment = null, bool defaultValue = false, bool consoleVisible = true, string prototype = null)
			: base (name, Boolean.TryParse, description, appSetting, environment, defaultValue, consoleVisible, prototype)
		{
		}
	}
}