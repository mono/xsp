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
using System.Security.Principal;
using Mono.WebServer.Log;
using System.Text;
using Mono.WebServer.FastCgi;
using Mono.Unix;
using System.Net.Sockets;
using Mono.WebServer.FastCgi.Compatibility;

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

		public static void SpawnShim (string shimCommand, string socket, string configFile, string fastCgiCommand) {
			if (configFile == null)
				throw new ArgumentNullException ("configFile");
			if (configFile.Length == 0)
				throw new ArgumentException ("Config file name can't be empty", "configFile");
			if (fastCgiCommand == null)
				throw new ArgumentNullException ("fastCgiCommand");
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = shimCommand,
					Arguments = String.Format ("{0} {1} --configfile \"{2}\" --ondemand", socket, fastCgiCommand, configFile),
					UseShellExecute = true
				}
			};
			Logger.Write (LogLevel.Debug, "Spawning shim \"{0} {1}\"", shimCommand, process.StartInfo.Arguments);
			process.Start ();
		}
	}
}
