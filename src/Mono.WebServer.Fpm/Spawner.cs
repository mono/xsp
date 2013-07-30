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

namespace Mono.WebServer.Fpm
{
	static class Spawner 
	{
		public static T RunAs<T, T1, T2, T3>(string user, Func<T1, T2, T3, T> action, T1 arg0, T2 arg1, T3 arg2)
		{
			if (user == null)
				throw new ArgumentNullException ("user");
			if (action == null)
				throw new ArgumentNullException ("action");
			using (var identity = new WindowsIdentity(user))
			using (identity.Impersonate())
				return action(arg0, arg1, arg2);
		}

		public static Process SpawnChild(string configFile, string fastCgiCommand, bool onDemand)
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
					Arguments = String.Format("--configfile \"{0}\"{1}", configFile, onDemand ? " --ondemand" : String.Empty),
					UseShellExecute = true
				}
			};
			process.Start();
			return process;
		}
	}
}
