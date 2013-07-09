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
				}
				catch (InvalidOperationException) {
					// Died between the if and the kill
				}
			}
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
		}

		public static void StartChildren(FileInfo[] configFiles, ConfigurationManager configurationManager)
		{
			foreach (var fileInfo in configFiles) {
				Logger.Write(LogLevel.Debug, "Loading {0}", fileInfo.Name);
				var childConfigurationManager = new ChildConfigurationManager();
				childConfigurationManager.LoadXmlConfig(fileInfo.FullName);
				string user = childConfigurationManager.User;
				string fastCgiCommand = configurationManager.FastCgiCommand;

				ChildInfo child;
				if (Platform.IsUnix) {
					if (String.IsNullOrEmpty(user)) {
						Logger.Write(LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner", fileInfo.Name);
						user = UnixFileSystemInfo.GetFileSystemEntry(fileInfo.FullName).OwnerUser.UserName;
					}

					child = Spawner.RunAs(user, Spawner.SpawnChild, fileInfo, fastCgiCommand);
				} else {
					Logger.Write(LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to the current one", fileInfo.Name);
					child = Spawner.SpawnChild(fileInfo, fastCgiCommand);
				}
				children.Add(child);
			}
		}
	}
}
