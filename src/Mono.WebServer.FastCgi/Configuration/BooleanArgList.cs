using System.Collections.Generic;

namespace Mono.WebServer.FastCgi.Configuration {
	class BooleanArgList : List<BooleanArg> {
		public void Add (string name, string description)
		{
			Add (new BooleanArg (name, description));
		}

		public void Add (string name, string description, string prototype)
		{
			Add (new BooleanArg (name, description, prototype));
		}
	}
}