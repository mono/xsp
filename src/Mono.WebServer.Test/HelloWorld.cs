using System;
using NUnit.Framework;
using Mono.WebServer.XSP;
using System.Reflection;
using System.IO;
using System.Net;

namespace Mono.WebServer.Test
{
	[TestFixture()]
	public class HelloWorld
	{
		[SetUp()]
		public void Init ()
		{
			// Force loading of the XSP assembly
			new SecurityConfiguration ();
			// and the WebServer one
			HttpErrors.BadRequest();

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

			var platform = Environment.OSVersion.Platform;
			string cut;
			// TODO: Write this in a portable way
			switch (platform) {
			case PlatformID.Unix:
				cut = assembly.CodeBase.Substring (7);
				break;
			default:
				cut = assembly.CodeBase.Substring (8);
				break;
			}
			string filename = Path.GetFileName (cut);

			string target = Path.Combine (binpath, filename);
			File.Copy (cut, target, true);
		}

		[Test]
		public void TestCase ()
		{
			var result = Server.Main (new []{"--applications", "/:.","--port", "9000","--nonstop"});
			Assert.AreEqual (0, result);
			var wc = new WebClient ();
			try {
				var downloaded = wc.DownloadString ("http://localhost:9000/");
				Assert.AreEqual (Environment.CurrentDirectory, downloaded);
			} catch (WebException e) {
				Assert.Fail(e.Message);
			}
		}
	}
}

