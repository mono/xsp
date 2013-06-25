namespace Mono.WebServer.Options {
	public class NullableSetting<T>:Setting<T?> where T : struct
	{
		public NullableSetting (string name, Parser<T> parser, string description, string appSetting = null, string environment = null, T? defaultValue = null, string prototype = null)
			: base (name, ToNullable(parser), description, appSetting, environment, defaultValue, prototype)
		{
		}

		static Parser<T?> ToNullable (Parser<T> parser)
		{
			return delegate (string input, out T? output)
			{
				T temp;
				if (!parser (input, out temp)) {
					output = null;
					return false;
				}
				output = temp;
				return true;
			};
		}
	}
}
