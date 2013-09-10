//
// Spawner.cs:
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
using System.Diagnostics;
using Mono.WebServer.Log;
using System.Text;
using Mono.WebServer.FastCgi;
using Mono.Unix;
using System.Net.Sockets;
using Mono.WebServer.FastCgi.Compatibility;
using System.IO;
using MonoDevelop.Core.Execution;

namespace Mono.WebServer.Fpm
{
	static class Spawner
	{
		static readonly byte[] spawnString = Encoding.UTF8.GetBytes ("SPAWN\n");
		static readonly BufferManager buffers = new BufferManager (100);

		public static Action RunAs(string user, string group, Action action)
		{
			if (user == null)
				throw new ArgumentNullException ("user");
			if (action == null)
				throw new ArgumentNullException ("action");
			return () => {
				try{
					using (Platform.Impersonate (user, group))
						action ();
				} catch (ArgumentException e) {
					Logger.Write (LogLevel.Error, "Couldn't run as {0} {1}!", user, group);
					Logger.Write (e);
				}
			};
		}

		public static Func<T> RunAs<T>(string user, string group, Func<T> action)
		{
			if (user == null)
				throw new ArgumentNullException ("user");
			if (action == null)
				throw new ArgumentNullException ("action");
			return () => {
				try{
					using (Platform.Impersonate (user, group))
						return action ();
				} catch (ArgumentException e) {
					Logger.Write (LogLevel.Error, "Couldn't run as {0} {1}!", user, group);
					Logger.Write (e);
					return default (T);
				}
			};
		}

		public static Process SpawnStaticChild (string configFile, string fastCgiCommand)
		{
			if (configFile == null)
				throw new ArgumentNullException ("configFile");
			if (configFile.Length == 0)
				throw new ArgumentException ("Config file name can't be empty", "configFile");
			if (fastCgiCommand == null)
				throw new ArgumentNullException ("fastCgiCommand");
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = fastCgiCommand,
					Arguments = String.Format ("--configfile \"{0}\"", configFile),
					UseShellExecute = true
				}
			};
			process.Start ();
			return process;
		}

		public static Process SpawnOndemandChild (string socketFile) {
			CompatArraySegment<byte>? torelease = null;
			try {
				Logger.Write (LogLevel.Debug, "Spawning via the shim {0}", socketFile);
				var client = new UnixClient ();
				client.Connect (socketFile);
				CompatArraySegment<byte> buffer = buffers.ClaimBuffer ();
				torelease = buffer;
				int receivedCount;
				using (NetworkStream socket = client.GetStream()) {
					socket.Write (spawnString, 0, spawnString.Length);
					receivedCount = socket.Read (buffer.Array, buffer.Offset, buffer.Count);
					if (receivedCount < 0)
						throw new Exception ("Didn't receive the child pid");
				}
				string received = Encoding.UTF8.GetString (buffer.Array, buffer.Offset, receivedCount);
				string clean = received.Trim ();
				int pid;
				if (!Int32.TryParse (clean, out pid))
					throw new Exception ("Couldn't parse the pid \"" + clean + "\"");

				if (pid < 0)
					throw new Exception ("Invalid pid: " + pid);

				return Process.GetProcessById (pid);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error, "Error while talking to the shim for socket file {0}", socketFile);
				Logger.Write (e);
				return null;
			} finally {
				if (torelease != null)
					buffers.ReturnBuffer (torelease.Value);
			}
		}

		public static void SpawnShim (ConfigurationManager configurationManager, string socket, string configFile) {
			if (configurationManager == null)
				throw new ArgumentNullException ("configurationManager");
			if (socket == null)
				throw new ArgumentNullException ("socket");
			if (configFile == null)
				throw new ArgumentNullException ("configFile");
			if (configFile.Length == 0)
				throw new ArgumentException ("Config file name can't be empty", "configFile");
			var builder = new ProcessArgumentBuilder ();
			builder.AddSingle (socket, GetFastCgiCommand (configurationManager.FastCgiCommand));
			builder.Add ("--ondemand");
			builder.AddFormatSafe ("--configfile '{0}'", configFile);
			var arguments = builder.ToString();

			var startInfo = new ProcessStartInfo {
				FileName = configurationManager.ShimCommand,
				Arguments = arguments,
				UseShellExecute = false
			};

			if ((configurationManager.LogLevels & LogLevel.Debug) != LogLevel.None)
				startInfo.EnvironmentVariables.Add ("DEBUG", "y");

			if (configurationManager.Verbose)
				startInfo.EnvironmentVariables.Add ("VERBOSE", "y");

			var process = new Process {
				StartInfo = startInfo
			};
			Logger.Write (LogLevel.Debug, "Spawning shim \"{0} {1}\"", configurationManager.ShimCommand, process.StartInfo.Arguments);
			process.Start ();
		}

		public static void SpawnShim (ConfigurationManager configurationManager, string shimSocket, string root, string onDemandSock) {
			if (configurationManager == null)
				throw new ArgumentNullException ("configurationManager");
			if (shimSocket == null)
				throw new ArgumentNullException ("shimSocket");
			if (root == null)
				throw new ArgumentNullException ("root");
			if (onDemandSock == null)
				throw new ArgumentNullException ("onDemandSock");

			var arguments = BuildArguments (configurationManager, shimSocket, root, onDemandSock);

			var startInfo = new ProcessStartInfo {
				FileName = configurationManager.ShimCommand,
				Arguments = arguments,
				UseShellExecute = false
			};

			if ((configurationManager.LogLevels & LogLevel.Debug) != LogLevel.None)
				startInfo.EnvironmentVariables.Add ("DEBUG", "y");

			if (configurationManager.Verbose)
				startInfo.EnvironmentVariables.Add ("VERBOSE", "y");

			var process = new Process {
				StartInfo = startInfo
			};

			Logger.Write (LogLevel.Debug, "Spawning shim \"{0} {1}\"", configurationManager.ShimCommand, process.StartInfo.Arguments);
			process.Start ();
		}

		static string GetFastCgiCommand (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");
			if (filename.Length == 0)
				throw new ArgumentException ("Filename can't be null for the fastcgi command", "filename");
			if (filename.StartsWith (Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				return filename;
			if (filename.Contains (Path.DirectorySeparatorChar.ToString()))
				return Path.Combine (Environment.CurrentDirectory, filename);
			string paths = Environment.GetEnvironmentVariable ("PATH");
			foreach (var path in paths.Split(Path.PathSeparator)) {
				string combined = Path.Combine (path, filename);
				if (File.Exists (combined) && IsExecutable (combined))
					return combined;
			}
			throw new ArgumentException (String.Format ("Couldn't find fastcgi executable at {0}", filename), "filename");
		}

		static bool IsExecutable (string path)
		{
			return Platform.IsUnix
				? new UnixFileInfo (path).CanAccess (Mono.Unix.Native.AccessModes.X_OK)
				: path.EndsWith (".exe", StringComparison.Ordinal);
		}

		static string BuildArguments (ConfigurationManager configurationManager, string shimSocket, string root, string onDemandSock)
		{
			var builder = new ProcessArgumentBuilder ();
			builder.AddSingle (shimSocket, GetFastCgiCommand (configurationManager.FastCgiCommand));
			if (configurationManager.Verbose)
				builder.Add ("--verbose");
			builder.Add ("--ondemand");
			builder.AddFormatSafe ("--applications /:'{0}'", root);
			builder.Add ("--idle-time", configurationManager.ChildIdleTime);
			builder.AddFormatSafe ("--ondemandsock 'unix://660@{0}'", onDemandSock);
			builder.AddFormat ("--loglevels {0}", configurationManager.LogLevels);
			builder.AddFormatSafe ("--name '{0}'", Path.GetFileName (root));
			var arguments = builder.ToString ();
			return arguments;
		}
	}
}
