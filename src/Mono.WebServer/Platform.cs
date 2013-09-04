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

		static void SetUser (string fpmUser)
		{
			UnixUserInfo fpm = null;
			try {
				fpm = new UnixUserInfo (fpmUser);
			} catch (ArgumentException e) {
				Logger.Write (e);
			}
			if (fpm != null) {
				var userId = fpm.UserId;
				if (userId > UInt32.MaxValue || userId <= 0)
					Logger.Write (LogLevel.Error, "Uid for {0} ({1}) not in range for suid", fpmUser, userId);
				Syscall.setuid ((uint)userId);
			}
		}

		static void SetGroup (string fpmGroup)
		{
			UnixGroupInfo fpm = null;
			try {
				fpm = new UnixGroupInfo (fpmGroup);
			} catch (ArgumentException e) {
				Logger.Write (e);
			}
			if (fpmGroup != null) {
				var groupId = fpm.GroupId;
				if (groupId > UInt32.MaxValue || groupId <= 0)
					Logger.Write (LogLevel.Error, "Gid for {0} ({1}) not in range for sgid", fpmGroup, groupId);
				Syscall.setgid ((uint)groupId);
			}
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
	}
}
