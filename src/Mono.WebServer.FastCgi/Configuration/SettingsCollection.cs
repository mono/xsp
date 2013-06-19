using System.Collections.ObjectModel;

namespace Mono.WebServer.FastCgi.Configuration {
	sealed class SettingsCollection : KeyedCollection<string, ISetting> {
		protected override string GetKeyForItem (ISetting item)
		{
			return item.Name;
		}
	}
}