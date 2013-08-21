using NDesk.Options;

namespace Mono.WebServer.Options {
	public interface IHelpConfigurationManager {
		string ProgramName { get; }
		string Description { get; }
		OptionSet CreateOptionSet ();
	}
}