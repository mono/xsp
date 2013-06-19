using System;

namespace Mono.WebServer.FastCgi.Configuration {
	class UInt16Setting : Setting<ushort> {
		public UInt16Setting (string name, string description, string appSetting = null, string environment = null, ushort defaultValue = default(UInt16), bool consoleVisible = true, string prototype = null)
			: base (name, UInt16.TryParse, description, appSetting, environment, defaultValue, consoleVisible, prototype)
		{
		}
	}
}