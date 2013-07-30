//
// ChildrenManager.cs:
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
using System.Collections.Generic;
using System.IO;
using Mono.Unix;
using Mono.WebServer.Log;
using System.Diagnostics;
using Mono.FastCgi;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer.Fpm
{
	static class ChildrenManager
	{
		static readonly List<ChildInfo> children = new List<ChildInfo>();

		public static void KillChildren()
		{
			foreach (ChildInfo child in children) {
				try {
					Process process = child.Process;
					if (process != null && !process.HasExited)
						process.Kill();
				} catch (InvalidOperationException) {
					// Died between the if and the kill
				}
			}
			children.RemoveAll(child => child.Process != null && child.Process.HasExited);
		}

		public static void TermChildren()
		{
			foreach (ChildInfo child in children) {
				try {
					Process process = child.Process;
					if (process != null && !process.HasExited)
						; //TODO: Write some nice close code
				} catch (InvalidOperationException) {
					// Died between the if and the kill
				}
			}
			children.RemoveAll (child => child.Process != null && child.Process.HasExited);
		}

		public static void StartChildren(FileInfo[] configFiles, ConfigurationManager configurationManager)
		{
			if (configFiles == null)
				throw new ArgumentNullException ("configFiles");
			if (configurationManager == null)
				throw new ArgumentNullException ("configurationManager");
			foreach (FileInfo fileInfo in configFiles) {
				if (fileInfo == null)
					continue;
				Logger.Write (LogLevel.Debug, "Loading {0}", fileInfo.Name);
				var childConfigurationManager = new ChildConfigurationManager ();
				string configFile = fileInfo.FullName;
				if (!childConfigurationManager.TryLoadXmlConfig (configFile))
					continue;
				string user = childConfigurationManager.User;
				string fastCgiCommand = configurationManager.FastCgiCommand;

				Func<bool, Process> spawner;
				if (Platform.IsUnix) {
					if (String.IsNullOrEmpty (user)) {
						Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner", fileInfo.Name);
						user = UnixFileSystemInfo.GetFileSystemEntry (configFile).OwnerUser.UserName;
					}

					spawner = onDemand => Spawner.RunAs (user, Spawner.SpawnChild, configFile, fastCgiCommand, onDemand);
				} else {
					Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to the current one", fileInfo.Name);
					spawner = onDemand => Spawner.SpawnChild (configFile, fastCgiCommand, onDemand);
				}
				var child = new ChildInfo { Spawner = spawner, ConfigurationManager = childConfigurationManager, Name = configFile, OnDemand = childConfigurationManager.InstanceType == InstanceType.Dynamic };
				children.Add (child);
				if (child.OnDemand){
					Socket socket;
					if (FastCgi.Server.TryCreateSocket (childConfigurationManager, out socket)) {
						var server = new GenericServer<Connection> (socket, child);
						server.Start (configurationManager.Stoppable, (int)childConfigurationManager.Backlog);
					}

				} else {
					if (child.TrySpawn ()) {
						Logger.Write (LogLevel.Notice, "Started fastcgi daemon [static] with pid {0} and config file {1}", child.Process.Id, Path.GetFileName (configFile));
					} else {
						Logger.Write (LogLevel.Error, "Couldn't start child with config file {0}", configFile);
					}
				}
			}
		}
	}
}
