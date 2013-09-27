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
using Mono.Unix.Native;

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

		static void CreateAutomaticDirs (string fpmGroup, string httpdGroup, out string shimSocketDir, out string frontSocketDir, out string backSocketDir)
		{
			string socketDir = Path.Combine (Path.GetTempPath (), "mono-fpm-automatic");
			CreateWithPerm (socketDir, "755");
			shimSocketDir = Path.Combine (socketDir, "shim");
			CreateWithPerm (shimSocketDir, "3733", fpmGroup);
			frontSocketDir = Path.Combine (socketDir, "front");
			CreateWithPerm (frontSocketDir, "2730", httpdGroup);
			backSocketDir = Path.Combine (socketDir, "back");
			CreateWithPerm (backSocketDir, "3733", fpmGroup);
		}

		public static void StartAutomaticChildren (IEnumerable<UnixDirectoryInfo> webDirs, ConfigurationManager configurationManager)
		{
			if (webDirs == null)
				throw new ArgumentNullException ("webDirs");
			if (configurationManager == null)
				throw new ArgumentNullException ("configurationManager");

			string shimSocketDir;
			string frontSocketDir;
			string backSocketDir;
			CreateAutomaticDirs (configurationManager.FpmGroup, configurationManager.HttpdGroup, out shimSocketDir, out frontSocketDir, out backSocketDir);
			foreach (UnixDirectoryInfo directoryInfo in webDirs) {
				if (directoryInfo == null)
					continue;

				var user = directoryInfo.OwnerUser.UserName;
				var group = directoryInfo.OwnerGroup.GroupName;
				var dirname = directoryInfo.Name;

				if (directoryInfo.OwnerUserId < 100) {
					Logger.Write (LogLevel.Debug, "Directory {0} skipped because owned by {1}:{2} ({3}:{4})",
					              dirname, user, group, directoryInfo.OwnerUserId, directoryInfo.OwnerGroupId);
					continue; // Skip non-user directories
				}

				string shimSocket = Path.Combine (shimSocketDir, dirname);
				string frontSocket = Path.Combine (frontSocketDir, dirname);
				string backSocket = Path.Combine (backSocketDir, dirname);

				Func<Process> spawner = () => Spawner.SpawnOndemandChild (shimSocket);
				UnixDirectoryInfo info = directoryInfo;
				Action spawnShim = () => Spawner.SpawnShim (configurationManager, shimSocket, info.FullName, backSocket);
				Spawner.RunAs (user, configurationManager.WebGroup, spawnShim) ();

				var child = new ChildInfo { Spawner = spawner, OnDemandSock =  backSocket, Name = directoryInfo.FullName };
				children.Add (child);
				
				PrepareAutomaticChild (configurationManager, frontSocket, child, 500);
			}
		}

		static void PrepareAutomaticChild (ConfigurationManager configurationManager, string frontSocket, ChildInfo child, uint backlog)
		{
			Socket socket;
			if (FastCgi.Server.TryCreateUnixSocket (frontSocket, out socket, "660")) {
				var server = new GenericServer<Connection> (socket, child);
				server.Start (configurationManager.Stoppable, (int)backlog);
			}
		}

		static void CreateWithPerm (string path, string permissions, string groupName = null)
		{
			Directory.CreateDirectory (path);
			uint perm = Convert.ToUInt32(permissions, 8);
			Syscall.chmod (path, NativeConvert.ToFilePermissions (perm));
			if (groupName == null)
				return;
			var group = new UnixGroupInfo (groupName);
			Syscall.chown (path, 0, (uint)group.GroupId);
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

				var spawner = GetSpawner (configurationManager, filename, childConfigurationManager, fullFilename);

				var child = new ChildInfo { Spawner = spawner, OnDemandSock = childConfigurationManager.OnDemandSock, Name = fullFilename };
				children.Add (child);

				PrepareChild (configurationManager, childConfigurationManager, fullFilename, child);
			}
		}

		static void PrepareChild (ConfigurationManager configurationManager, ChildConfigurationManager childConfigurationManager, string fullFilename, ChildInfo child)
		{
			if (childConfigurationManager.InstanceType == InstanceType.Static) {
				if (child.TrySpawn ()) {
					Logger.Write (LogLevel.Notice, "Started fastcgi daemon [static] with pid {0} and config file {1}", child.Process.Id, Path.GetFileName (fullFilename));
					Thread.Sleep (500);
					// TODO: improve this (it's used to wait for the child to be ready)
				}
				else
					Logger.Write (LogLevel.Error, "Couldn't start child with config file {0}", fullFilename);
			}
			else {
				Socket socket;
				if (FastCgi.Server.TryCreateSocket (childConfigurationManager, out socket)) {
					var server = new GenericServer<Connection> (socket, child);
					server.Start (configurationManager.Stoppable, (int)childConfigurationManager.Backlog);
				}
			}
		}

		static Func<Process> GetSpawner (ConfigurationManager configurationManager, string filename, ChildConfigurationManager childConfigurationManager, string configFile)
		{
			Func<Process> spawner;
			if (childConfigurationManager.InstanceType == InstanceType.Ondemand) {
				if (String.IsNullOrEmpty (childConfigurationManager.ShimSocket))
					throw new Exception ("You must specify a socket for the shim");
				spawner = () => Spawner.SpawnOndemandChild (childConfigurationManager.ShimSocket);
			}
			else
				spawner = () => Spawner.SpawnStaticChild (configFile, configurationManager.FastCgiCommand);

			Action spawnShim = () => Spawner.SpawnShim (configurationManager, childConfigurationManager.ShimSocket, configFile);
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
