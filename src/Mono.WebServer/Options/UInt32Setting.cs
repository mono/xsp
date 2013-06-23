using System;

namespace Mono.WebServer.Options {
	public class UInt32Setting : Setting<uint>
	{
		public UInt32Setting (string name, string description, string appSetting = null, string environment = null, uint defaultValue = default(UInt32), string prototype = null)
			: base (name, UInt32.TryParse, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}