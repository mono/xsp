using System.Collections.ObjectModel;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer {
	sealed class SettingsCollection : KeyedCollection<string, SettingInfo> {
		protected override string GetKeyForItem (SettingInfo item)
		{
			return item.Name;
		}
	}
}