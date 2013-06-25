using System;

namespace Mono.WebServer.Options {
	public class UInt16Setting : Setting<ushort>
	{
		public UInt16Setting (string name, string description, string appSetting = null, string environment = null, ushort defaultValue = default(UInt16), string prototype = null)
			: base (name, UInt16.TryParse, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}