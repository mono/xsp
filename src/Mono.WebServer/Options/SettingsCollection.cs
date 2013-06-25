using System.Collections.ObjectModel;

namespace Mono.WebServer.Options {
	public sealed class SettingsCollection : KeyedCollection<string, ISetting> {
		protected override string GetKeyForItem (ISetting item)
		{
			return item.Name;
		}
	}
}