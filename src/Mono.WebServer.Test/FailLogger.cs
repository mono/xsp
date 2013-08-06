using Mono.WebServer.Log;
using NUnit.Framework;

namespace Mono.WebServer.Test
{
	public class FailLogger : ILogger
	{
		public void Write (LogLevel level, string text)
		{
			if ((level & LogLevel.Error) != LogLevel.None)
				Assert.Fail (text);
		}
	}
}

