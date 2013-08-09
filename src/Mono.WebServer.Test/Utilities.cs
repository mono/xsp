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

