using System;
using System.IO;
using System.Reflection;

namespace Mono.WebServer {
	public class Version
	{
		public static void Show ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string version = assembly.GetName ().Version.ToString ();

			// string title = GetAttribute<AssemblyTitleAttribute> (a => a.Title);
			string copyright = GetAttribute<AssemblyCopyrightAttribute> (a => a.Copyright);
			string description = GetAttribute<AssemblyDescriptionAttribute> (a => a.Description);

			Console.WriteLine ("{0} {1}\n(c) {2}\n{3}",
				Path.GetFileName (assembly.Location), version,
				copyright, description);
		}

		static string GetAttribute<T> (Func<T, string> func) where T : class
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			var attributes = assembly.GetCustomAttributes (typeof (T), false);
			if (attributes.Length == 0)
				return String.Empty;
			var att = attributes [0] as T;
			if (att == null)
				return String.Empty;
			return func (att);
		}
	}
}
