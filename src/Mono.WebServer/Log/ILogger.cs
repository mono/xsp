namespace Mono.WebServer.Log
{
	public interface ILogger
	{
		void Write (LogLevel level, string text);
	}
}
