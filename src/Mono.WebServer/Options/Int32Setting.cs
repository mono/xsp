using System;

namespace Mono.WebServer.Options {
	public class Int32Setting : Setting<int>
	{
		public Int32Setting (string name, string description, string appSetting = null, string environment = null, int defaultValue = default(Int32), string prototype = null)
			: base (name, Int32.TryParse, description, appSetting, environment, defaultValue, prototype)
		{
		}
	}
}