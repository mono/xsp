using System;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;
using Mono.WebServer.Log;

namespace Mono.WebServer.Fpm
{
	static class ChildrenManager
	{
		static readonly List<ChildInfo> children = new List<ChildInfo>();

		public static void KillChildren()
		{
			foreach (ChildInfo child in children) {
				try {
					if (!child.Process.HasExited)
						child.Process.Kill();
				} catch (InvalidOperationException) {
					// Died between the if and the kill
				}
			}
			children.RemoveAll(child => child.Process.HasExited);
		}

		public static void TermChildren()
		{
			foreach (ChildInfo child in children) {
				try {
					if (!child.Process.HasExited)
						; //TODO: Write some nice close code
				} catch (InvalidOperationException) {
					// Died between the if and the kill
				}
			}
			children.RemoveAll (child => child.Process.HasExited);
		}

		public static void StartChildren(FileInfo[] configFiles, ConfigurationManager configurationManager)
		{
			if (configFiles == null)
				throw new ArgumentNullException ("configFiles");
			if (configurationManager == null)
				throw new ArgumentNullException ("configurationManager");
			foreach (var fileInfo in configFiles) {
				Logger.Write(LogLevel.Debug, "Loading {0}", fileInfo.Name);
				var childConfigurationManager = new ChildConfigurationManager();
				string fullName = fileInfo.FullName;
				childConfigurationManager.LoadXmlConfig(fullName);
				string user = childConfigurationManager.User;
				string fastCgiCommand = configurationManager.FastCgiCommand;

				ChildInfo child;
				if (Platform.IsUnix) {
					if (String.IsNullOrEmpty(user)) {
						Logger.Write(LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner", fileInfo.Name);
						user = UnixFileSystemInfo.GetFileSystemEntry(fullName).OwnerUser.UserName;
					}

					child = Spawner.RunAs(user, Spawner.SpawnChild, fullName, fastCgiCommand);
				} else {
					Logger.Write(LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to the current one", fileInfo.Name);
					child = Spawner.SpawnChild(fullName, fastCgiCommand);
				}
				children.Add(child);
			}
		}
	}
}
