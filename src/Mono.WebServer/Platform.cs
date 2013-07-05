using System;
using System.IO;

namespace Mono.WebServer {
	public static class Platform {
		public static string Name {
			get { return Value.ToString (); }
		}

		static FinePlatformID Value {
			get {
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
		}

		public static bool IsUnix {
			get {
				var platform = (int)Environment.OSVersion.Platform;
				return platform == 4 || platform == 6 || platform == 128;
			}
		}
	}
}
