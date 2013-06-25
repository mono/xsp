namespace Mono.WebServer.Options {
	public abstract partial class ConfigurationManager
	{
		protected void Add (params ISetting[] settings)
		{
			foreach (ISetting setting in settings)
				Settings.Add (setting);
		}
	}
}
