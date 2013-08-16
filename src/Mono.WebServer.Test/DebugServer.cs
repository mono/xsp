using System;
using Mono.WebServer.XSP;

namespace Mono.WebServer.Test
{
	public class DebugServer : IDisposable
	{
		ApplicationServer server;

		public void Dispose ()
		{
			if (server != null)
				server.Stop ();
		}

		public int Run ()
		{
			Tuple<int, string, ApplicationServer> res = Server.DebugMain (new [] { "--applications", "/:.", "--port", "9000", "--nonstop" });
			server = res.Item3;
			return res.Item1;
		}
	}
}
