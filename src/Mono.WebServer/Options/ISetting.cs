using System;

namespace Mono.WebServer.Options {
	public interface ISetting
	{
		string Name { get; }
		bool ConsoleVisible { get; }
		string Description { get; }
		string AppSetting { get; }
		string Environment { get; }
		string Prototype { get; }
		[Obsolete]
		object Value { get; }

		void MaybeParseUpdate (SettingSource settingSource, string value);
	}
}