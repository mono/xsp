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
using System.Threading;

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
					if (process != null && !process.HasExited) {
						//TODO: Write some nice close code
					}
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
				var filename = fileInfo.Name;
				var childConfigurationManager = new ChildConfigurationManager ("child-" + filename);
				Logger.Write (LogLevel.Debug, "Loaded {0} [{1}]", filename, childConfigurationManager.InstanceType.ToString ().ToLowerInvariant ());
				string fullFilename = fileInfo.FullName;
				if (!childConfigurationManager.TryLoadXmlConfig (fullFilename))
					continue;

				var spawner = GetSpawner (configurationManager.ShimCommand, filename, childConfigurationManager, fullFilename, configurationManager.FastCgiCommand);

				var child = new ChildInfo { Spawner = spawner, ConfigurationManager = childConfigurationManager, Name = fullFilename };
				children.Add (child);

				if (childConfigurationManager.InstanceType == InstanceType.Static) {
					if (child.TrySpawn ()) {
						Logger.Write (LogLevel.Notice, "Started fastcgi daemon [static] with pid {0} and config file {1}", child.Process.Id, Path.GetFileName (fullFilename));
						Thread.Sleep (500);
						// TODO: improve this (it's used to wait for the child to be ready)
					} else
						Logger.Write (LogLevel.Error, "Couldn't start child with config file {0}", fullFilename);
					break;
				} else {
					Socket socket;
					if (FastCgi.Server.TryCreateSocket (childConfigurationManager, out socket)) {
						var server = new GenericServer<Connection> (socket, child);
						server.Start (configurationManager.Stoppable, (int)childConfigurationManager.Backlog);
					}
				}
			}
		}

		static Func<Process> GetSpawner (string shimCommand, string filename, ChildConfigurationManager childConfigurationManager, string configFile, string fastCgiCommand)
		{
			Func<Process> spawner;
			if (childConfigurationManager.InstanceType == InstanceType.Ondemand) {
				if (String.IsNullOrEmpty (childConfigurationManager.ShimSocket))
					throw new Exception ("You must specify a socket for the shim");
				spawner = () => Spawner.SpawnOndemandChild (childConfigurationManager.ShimSocket);
			}
			else
				spawner = () => Spawner.SpawnStaticChild (configFile, fastCgiCommand);

			Action spawnShim = () => Spawner.SpawnShim (shimCommand, childConfigurationManager.ShimSocket, configFile, fastCgiCommand);
			string user = childConfigurationManager.User;
			string group = childConfigurationManager.Group;
			if (String.IsNullOrEmpty (user)) {
				if (Platform.IsUnix) {
					Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to file owner", filename);
					string owner = UnixFileSystemInfo.GetFileSystemEntry (configFile).OwnerUser.UserName;
					if (childConfigurationManager.InstanceType == InstanceType.Ondemand)
						Spawner.RunAs (owner, group, spawnShim) ();
					else
						spawner = Spawner.RunAs (owner, group, spawner);
				}
				else {
					Logger.Write (LogLevel.Warning, "Configuration file {0} didn't specify username, defaulting to the current one", filename);
					if (childConfigurationManager.InstanceType != InstanceType.Ondemand)
						spawnShim ();
				}
			}
			else {
				if (childConfigurationManager.InstanceType == InstanceType.Ondemand)
					Spawner.RunAs (user, group, spawnShim) ();
				else
					spawner = Spawner.RunAs (user, group, spawner);
			}

			return spawner;
		}
	}
}
