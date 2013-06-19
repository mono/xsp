using System;
using System.IO;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer {
	public partial class ConfigurationManager {
		public object this [string name] {
			get {
				SettingInfo setting;
				string value = TryGetString (name, out setting);
				
				if (setting == null)
					throw AppExcept (EXCEPT_UNREGISTERED, name);
				
				SettingInfo.SettingType type = setting.Type;
				
				if (value == null)
					switch (type) {
					case SettingInfo.SettingType.UInt16:
						return default(UInt16);
					case SettingInfo.SettingType.Bool:
						return false;
					default:
						return null;
					}
				
				switch (type) {
				case SettingInfo.SettingType.String:
					return value;
					
				case SettingInfo.SettingType.UInt16:
					try {
						return UInt16.Parse (value);
					} catch (Exception except) {
						throw AppExcept (except, EXCEPT_UINT16, name, value);
					}
					
				case SettingInfo.SettingType.Bool:
					bool result;
					if (bool.TryParse (value, out result))
						return result;

					throw AppExcept (EXCEPT_BOOL, name, value);
					
				case SettingInfo.SettingType.Directory:
					if (Directory.Exists (value))
						return value;
					
					throw AppExcept (EXCEPT_DIRECTORY, name, value);
					
				case SettingInfo.SettingType.File:
					if (File.Exists (value))
						return value;
					
					throw AppExcept (EXCEPT_FILE, name, value);

				// TODO: Probably not needed anymore
				default:
					throw AppExcept (EXCEPT_UNKNOWN, name, type);
				}
			}
			
			set {
				cmd_args.AddOrReplace (name, value.ToString ());
			}
		}
	}
}
