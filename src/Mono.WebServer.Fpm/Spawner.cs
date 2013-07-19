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
			if (user == null)
				throw new ArgumentNullException ("user");
			if (action == null)
				throw new ArgumentNullException ("action");
			using (var identity = new WindowsIdentity(user))
			using (identity.Impersonate())
				return action(arg0, arg1);
		}

		public static ChildInfo SpawnChild(string fullName, string fastCgiCommand)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");
			if (fastCgiCommand == null)
				throw new ArgumentNullException ("fastCgiCommand");
			var info = new ChildInfo {
				Process = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = fastCgiCommand,
						Arguments = String.Format("--config-file \"{0}\"", fullName),
						UseShellExecute = true
					}
				}
			};
			info.Process.Start();
			return info;
		}
	}
}
