using System.Collections.ObjectModel;

namespace Mono.WebServer.FastCgi {
	sealed class SettingsCollection : KeyedCollection<string, ISetting> {
		protected override string GetKeyForItem (ISetting item)
		{
			return item.Name;
		}
	}
}