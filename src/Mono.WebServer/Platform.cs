using System;
using System.IO;
using Mono.Unix;
using Mono.WebServer.Log;
using Mono.Unix.Native;

namespace Mono.WebServer {
	public static class Platform
	{
		public static string Name {
			get { return Value.ToString (); }
		}

		static FinePlatformID Value {
			get {
				switch (Environment.OSVersion.Platform) {
					case PlatformID.Unix:
					case (PlatformID)128:
					// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
					// Instead of platform check, we'll do a feature checks (Mac specific root folders)
						if (Directory.Exists ("/Applications")
						   && Directory.Exists ("/System")
						   && Directory.Exists ("/Users")
						   && Directory.Exists ("/Volumes"))
							return FinePlatformID.MacOSX;
						return FinePlatformID.Linux;

					case PlatformID.MacOSX:
						return FinePlatformID.MacOSX;

					default:
						return FinePlatformID.Windows;
				}
			}
		}

		public static bool IsUnix {
			get {
				var platform = (int)Environment.OSVersion.Platform;
				return platform == 4 || platform == 6 || platform == 128;
			}
		}

		static uint? GetUid (string user)
		{
			UnixUserInfo info = null;
			try {
				info = new UnixUserInfo (user);
			} catch (ArgumentException e) {
				Logger.Write (e);
			}
			if (info != null) {
				long uid = info.UserId;
				if (uid <= UInt32.MaxValue && uid > 0)
					return (uint)uid;
				Logger.Write (LogLevel.Error, "Uid for {0} ({1}) not in range for suid", user, uid);
			}
			return null;
		}

		static uint? GetGid (string group)
		{
			UnixGroupInfo info = null;
			try {
				info = new UnixGroupInfo (group);
			} catch (ArgumentException e) {
				Logger.Write (e);
			}
			if (info != null) {
				var gid = info.GroupId;
				if (gid <= UInt32.MaxValue && gid > 0)
					return (uint)gid;
				Logger.Write (LogLevel.Error, "Gid for {0} ({1}) not in range for sgid", group, gid);
			}
			return null;
		}

		static void SetUser (string user)
		{
			uint? gid = GetUid (user);
			if (gid != null)
				Syscall.setuid (gid.Value);
		}

		static void SetGroup (string group)
		{
			var uid = GetGid (group);
			if (uid != null)
				Syscall.setgid (uid.Value);
		}

		public static void LogIdentity ()
		{
			Logger.Write (LogLevel.Debug, "Uid {0}, euid {1}, gid {2}, egid {3}", Syscall.getuid (), Syscall.geteuid (), Syscall.getgid (), Syscall.getegid ());
		}

		public static void SetIdentity (uint uid, uint gid)
		{
			// TODO: Use platform-specific code
			if (Platform.IsUnix) {
				Syscall.setgid (gid);
				Syscall.setuid (uid);
				LogIdentity ();
			} else
				Logger.Write (LogLevel.Warning, "Not dropping privileges");
		}

		public static void SetIdentity (string user, string group)
		{
			// TODO: Use platform-specific code
			if (Platform.IsUnix) {
				SetGroup (group);
				SetUser (user);
				LogIdentity ();
			} else
				Logger.Write (LogLevel.Warning, "Not dropping privileges");
		}

		public static IDisposable Impersonate(string user,string group)
		{
			uint? uid = GetUid (user);
			uint? gid = GetGid (group);
			if (uid != null && gid != null) {
				uint euid = Syscall.geteuid ();
				uint egid = Syscall.getegid ();
				Syscall.setegid (gid.Value);
				Syscall.seteuid (uid.Value);
				return new IdentityToken(euid, egid);
			}
			return null;
		}
	}
}
