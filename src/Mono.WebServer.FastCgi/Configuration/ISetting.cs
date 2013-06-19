using System;

namespace Mono.WebServer.FastCgi.Configuration {
	interface ISetting {
		string Name { get; }
		bool ConsoleVisible { get; }
		string Description { get; }
		string AppSetting { get; }
		string Environment { get; }
		string Prototype { get; }

		void MaybeParseUpdate (SettingSource settingSource, string value);

		[Obsolete]
		object ObjectValue { get; }
	}
}