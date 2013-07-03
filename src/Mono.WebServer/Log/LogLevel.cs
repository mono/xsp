using System;

namespace Mono.WebServer.Log {
	[Flags]
	public enum LogLevel
	{
		None     = 0x00,
		Error    = 0x01,
		Warning  = 0x02,
		Notice   = 0x04,
		Debug    = 0x08,
		Standard = Error | Warning | Notice,
		All     = Error | Warning | Notice | Debug
	}
}