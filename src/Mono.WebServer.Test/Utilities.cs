//
// Utilities.cs
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
using Mono.WebServer.XSP;
using Mono.FastCgi;
using System.IO;
using System.Reflection;
using Mono.WebServer.Log;

namespace Mono.WebServer.Test {
	public static class Utilities
	{
		public static void SetLogToFail ()
		{
			Logger.AddLogger (new FailLogger ());
		}

		public static void LoadAssemblies ()
		{
			// Force loading of the XSP assembly
			new SecurityConfiguration ();
			// FastCgi
			new NameValuePair ();
			// and the WebServer one
			HttpErrors.BadRequest();
		}

		public static void CopyLoadedAssemblies()
		{
			string binpath = Path.Combine (Environment.CurrentDirectory, "bin");
			if (!Directory.Exists (binpath))
				Directory.CreateDirectory (binpath);

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies ();
			foreach (Assembly assembly in assemblies)
				MaybeCopyAssembly (assembly, binpath);
		}

		static void MaybeCopyAssembly (Assembly assembly, string binpath)
		{
			if (assembly.GlobalAssemblyCache || assembly.CodeBase == null)
				return;

			string cut = assembly.CodeBase.Substring (Platform.IsUnix ? 7 : 8);
			string filename = Path.GetFileName (cut);

			string target = Path.Combine (binpath, filename);
			try{
				File.Copy (cut, target, true);
			}
			catch(UnauthorizedAccessException){
				try {
					File.ReadAllText (cut);
				}
				catch (UnauthorizedAccessException i) {
					throw new Exception ("Couldn't read source", i);
				}
				try {
					File.WriteAllText (target, "WOLOLO");
				}
				catch (UnauthorizedAccessException i) {
					throw new Exception ("Couldn't write dest", i);
				}
				throw;
			}
		}
	}
}

