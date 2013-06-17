using System;
using NUnit.Framework;
using Mono.WebServer.XSP;
using System.Reflection;
using System.IO;
using System.Net;
using System.Threading;

namespace Mono.WebServer.Test
{
	[TestFixture()]
	public class Test
	{
		[SetUp()]
		public void Init ()
		{
			// Force loading of the XSP assembly
			new SecurityConfiguration ();
			// and the WebServer one
			HttpErrors.BadRequest();
			
			rpath = Environment.CurrentDirectory;

			string binpath = Path.Combine (rpath, "bin");
			if (!Directory.Exists (binpath))
				Directory.CreateDirectory (binpath);

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies ();
			foreach (Assembly assembly in assemblies) {
				if (assembly.GlobalAssemblyCache || assembly.CodeBase == null)
					continue;
				
				string cut = assembly.CodeBase.Substring (7);
				string filename = Path.GetFileName (cut);
				string target = Path.Combine (binpath, filename);
				File.Copy (cut, target, true);
			}
		}
		
		private string rpath;
		
		public static Tuple<Server,string> ServerMain (string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += Server.CurrentDomain_UnhandledException;
			bool quiet = false;
			while (true) {
				try {
					var server = new Server ();
					string result = server.DebugMain (args, true, null, quiet);
					return new Tuple<Server, string>(server, result);
				} catch (ThreadAbortException ex) {
					Console.WriteLine (ex);
					// Single-app mode and ASP.NET appdomain unloaded
					Thread.ResetAbort ();
					quiet = true; // hush 'RealMain'
				}
			}
		}
		
		[Test()]
		public void TestCase ()
		{
			var serverCouple = ServerMain (new []{"--applications", "/:" + rpath,"--port", "9000"});
			Assert.AreEqual (null, serverCouple.Item2);
			var wc = new WebClient ();
			var result = wc.DownloadString ("http://localhost:9000/");
			Assert.AreEqual (rpath, result);
			serverCouple.Item1.Stop ();
		}
	}
}

