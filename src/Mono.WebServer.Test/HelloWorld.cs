using System;
using NUnit.Framework;
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
			using (var server = new DebugServer()) {
				Assert.AreEqual (0, server.Run ());
				var wc = new WebClient ();
				try {
					string downloaded = wc.DownloadString ("http://localhost:9000/");
					Assert.AreEqual (Environment.CurrentDirectory, downloaded);
				} catch (WebException e) {
					Assert.Fail (e.Message);
				}
			}
		}
	}
}

