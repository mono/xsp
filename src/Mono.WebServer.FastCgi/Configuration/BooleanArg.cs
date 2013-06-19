namespace Mono.WebServer.FastCgi.Configuration {
	class BooleanArg {
		public string Name { get; private set; }
		public string Description { get; private set; }
		public string Prototype { get; private set; }

		public BooleanArg (string name, string description)
		{
			Name = name;
			Description = description;
			Prototype = name;
		}

		public BooleanArg (string name, string description, string prototype)
		{
			Name = name;
			Description = description;
			Prototype = prototype;
		}
	}
}