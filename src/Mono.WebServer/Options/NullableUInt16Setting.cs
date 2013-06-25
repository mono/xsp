using System;

namespace Mono.WebServer.Options {
	public class NullableUInt16Setting : NullableSetting<ushort>
	{
		public NullableUInt16Setting (string name, string description, string appSetting = null, string environment = null, ushort? defaultValue = null, string prototype = null)
			: base (name, UInt16.TryParse, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}