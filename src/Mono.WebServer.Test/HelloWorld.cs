using System;
using NUnit.Framework;
using Mono.WebServer.XSP;
using System.Reflection;
using System.IO;
using System.Net;

namespace Mono.WebServer.Test
{
	[TestFixture]
	public class HelloWorld
	{
		[SetUp]
		public void Init ()
		{
			Utilities.LoadAssemblies ();
			Utilities.CopyLoadedAssemblies ();
			Utilities.SetLogToFail ();
		}

		[Test]
		public void TestCase ()
		{
			int result = Server.Main (new []{"--applications", "/:.","--port", "9000","--nonstop"});
			Assert.AreEqual (0, result);
			var wc = new WebClient ();
			try {
				string downloaded = wc.DownloadString ("http://localhost:9000/");
				Assert.AreEqual (Environment.CurrentDirectory, downloaded);
			} catch (WebException e) {
				Assert.Fail(e.Message);
			}
		}
	}
}

