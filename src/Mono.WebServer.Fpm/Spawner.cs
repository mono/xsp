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
using Mono.FastCgi;
using System.Text;
using Mono.WebServer.FastCgi;

namespace Mono.WebServer.Fpm
{
	static class Spawner 
	{
		static byte[] spawnString = Encoding.UTF8.GetBytes ("SPAWN\n");
		static BufferManager buffers = new BufferManager (100);

		public static Action RunAs(string user, Action action)
		{
			if (user == null)
				throw new ArgumentNullException ("user");
			if (action == null)
				throw new ArgumentNullException ("action");
			return () => {
				using (var identity = new WindowsIdentity(user))
					using (identity.Impersonate())
						action ();
			};
		}

		public static Func<T> RunAs<T>(string user, Func<T> action)
		{
			if (user == null)
				throw new ArgumentNullException ("user");
			if (action == null)
				throw new ArgumentNullException ("action");
			return () => {
				using (var identity = new WindowsIdentity(user))
				using (identity.Impersonate())
					return action ();
			};
		}

		public static Process SpawnChild (string configFile, string fastCgiCommand, InstanceType type)
		{
			if (configFile == null)
				throw new ArgumentNullException ("configFile");
			if (configFile.Length == 0)
				throw new ArgumentException ("Config file name can't be empty", "configFile");
			if (fastCgiCommand == null)
				throw new ArgumentNullException ("fastCgiCommand");
			switch (type) {
			case InstanceType.Static:
			case InstanceType.Dynamic:
				var process = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = fastCgiCommand,
						Arguments = String.Format (type == InstanceType.Dynamic ? "--configfile \"{0}\" --ondemand" : "--configfile \"{0}\"", configFile),
						UseShellExecute = true
					}
				};
				process.Start ();
				return process;
			default:
				throw new ArgumentOutOfRangeException ("type");
			}
		}

		public static Process SpawnOndemandChild (string socketFile) {
				Socket socket = null;
			bool connected = false;
			try {
				Logger.Write (LogLevel.Debug, "Spawning via the shim {0}", socketFile);
				if (!Mono.WebServer.FastCgi.Server.TryCreateUnixSocket (socketFile, out socket))
					throw new Exception ("Couldn't create the socket " + socketFile);
				socket.Connect ();
				connected = true;
				socket.Send (spawnString, 0, spawnString.Length, System.Net.Sockets.SocketFlags.None);
				var buffer = buffers.ClaimBuffer ();
				var receivedCount = socket.Receive (buffer.Array, buffer.Offset, buffer.Count, System.Net.Sockets.SocketFlags.None);
				if (receivedCount < 0)
					throw new Exception ("Didn't receive the child pid");
				string received = Encoding.UTF8.GetString (buffer.Array, buffer.Offset, receivedCount);
				int pid;
				if (!Int32.TryParse (received, out pid))
					throw new Exception ("Couldn't parse the pid \"" + received + "\"");

				if (pid < 0)
					throw new Exception ("Invalid pid: " + pid);

				return Process.GetProcessById (pid);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error, "Error while talking to the shim for socket file {0}", socketFile);
				Logger.Write (e);
				return null;
			} finally {
				if (connected && socket != null)
					socket.Close ();
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
