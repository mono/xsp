using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace Mono.WebServer.Fpm
{
	static class Spawner 
	{
		public static T RunAs<T, T1, T2>(string user, Func<T1, T2, T> action, T1 arg0, T2 arg1)
		{
			using (var identity = new WindowsIdentity(user))
			using (identity.Impersonate())
				return action(arg0, arg1);
		}

		public static ChildInfo SpawnChild(FileInfo fileInfo, string fastCgiCommand)
		{
			var info = new ChildInfo {
				Process = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = fastCgiCommand,
						Arguments = String.Format("--config-file {0}", fileInfo.FullName),
						UseShellExecute = true
					}
				}
			};
			info.Process.Start();
			return info;
		}
	}
}
