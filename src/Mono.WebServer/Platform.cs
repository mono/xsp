//
// Platform.cs
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (c) 2013 Leonardo Taglialegne.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using Mono.Unix;
using Mono.Unix.Native;
using Mono.WebServer.Log;

namespace Mono.WebServer {
	public static class Platform
	{
		public static readonly FinePlatformID Value;

		public static string Name {
			get { return Value.ToString (); }
		}
		
		public static bool IsUnix {
			get {
				var platform = Environment.OSVersion.Platform;
				return platform == PlatformID.Unix || platform == PlatformID.MacOSX || platform == (PlatformID)128;
			}
		}

		static Platform() {
			Value = GetPlatformId();
		}

		static FinePlatformID GetPlatformId() {
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Unix:
				case (PlatformID)128:
					// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
					// Instead of platform check, we'll do a feature checks (Mac specific root folders)
					if (Directory.Exists("/Applications")
					   && Directory.Exists("/System")
					   && Directory.Exists("/Users")
					   && Directory.Exists("/Volumes"))
						return FinePlatformID.MacOSX;
					return FinePlatformID.Linux;

				case PlatformID.MacOSX:
					return FinePlatformID.MacOSX;

				default:
					return FinePlatformID.Windows;
			}
		}

		static uint GetUid (string user)
		{
			var info = new UnixUserInfo (user);
			long uid = info.UserId;
			if (uid > UInt32.MaxValue || uid <= 0)
				throw new ArgumentOutOfRangeException ("user", String.Format ("Uid for {0} ({1}) not in range for suid", user, uid));
			return (uint)uid;
		}

		static uint GetGid (string group)
		{
			var info = new UnixGroupInfo (group);
			var gid = info.GroupId;
			if (gid > UInt32.MaxValue || gid <= 0)
				throw new ArgumentOutOfRangeException ("group", String.Format ("Gid for {0} ({1}) not in range for sgid", group, gid));
			return (uint)gid;
		}

		static void SetUser (string user)
		{
			Syscall.setuid (GetUid (user));
		}

		static void SetGroup (string group)
		{
			Syscall.setgid (GetGid (group));
		}

		public static void LogIdentity ()
		{
			Logger.Write (LogLevel.Debug, "Uid {0}, euid {1}, gid {2}, egid {3}", Syscall.getuid (), Syscall.geteuid (), Syscall.getgid (), Syscall.getegid ());
		}

		public static void SetIdentity (uint uid, uint gid)
		{
			// TODO: Use platform-specific code
			if (IsUnix) {
				Syscall.setgid (gid);
				Syscall.setuid (uid);
				LogIdentity ();
			} else
				Logger.Write (LogLevel.Warning, "Not dropping privileges");
		}

		public static void SetIdentity (string user, string group)
		{
			// TODO: Use platform-specific code
			if (IsUnix) {
				SetGroup (group);
				SetUser (user);
				LogIdentity ();
			} else
				Logger.Write (LogLevel.Warning, "Not dropping privileges");
		}

		public static IDisposable Impersonate(string user,string group)
		{
			uint uid = GetUid (user);
			uint gid = GetGid (group);
			uint euid = Syscall.geteuid ();
			uint egid = Syscall.getegid ();
			Syscall.setegid (gid);
			Syscall.seteuid (uid);
			return new IdentityToken (euid, egid);
		}
	}
}
