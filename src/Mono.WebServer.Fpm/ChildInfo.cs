using System.Diagnostics;

namespace Mono.WebServer.Fpm {
	class ChildInfo 
	{
		public Process Process { get; set; }

		public ChildInfo (Process process = null)
		{
			Process = process;
		}
	}
}