using NDesk.Options;

namespace Mono.WebServer.Options {
	public interface IHelpConfigurationManager {
		string Name { get; }
		string Description { get; }
		OptionSet CreateOptionSet ();
	}
}