using System.Diagnostics;

namespace Mono.WebServer.Fpm {
	class ChildInfo 
	{
		public ChildInfo (Process process = null)
		{
			Process = process;
		}

		public Process Process { get; set; }
	}
}