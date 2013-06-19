using System;

namespace Mono.WebServer {
	public class ConfigurationManager {
		readonly FastCgi.ConfigurationManager configurationManager = new FastCgi.ConfigurationManager ();

		[Obsolete]
		public object this [string name] {
			get {
				return configurationManager.GetSetting (name).ObjectValue;
			}
			
			set {
				configurationManager.SetValue (name, value);
			}
		}
	}
}
