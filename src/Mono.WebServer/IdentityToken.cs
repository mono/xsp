using System;
using Mono.Unix.Native;

namespace Mono.WebServer
{
	public class IdentityToken : IDisposable
	{
		readonly uint euid;
		readonly uint egid;

		public IdentityToken (uint euid, uint egid)
		{
			this.euid = euid;
			this.egid = egid;
		}

		public void Dispose ()
		{
			Syscall.seteuid (euid);
			Syscall.setegid (egid);
		}
	}
}
