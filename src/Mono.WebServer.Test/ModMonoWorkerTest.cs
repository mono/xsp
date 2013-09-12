using NUnit.Framework;
using System;
using Mono.WebServer.Apache;
using System.IO;

namespace Mono.WebServer.Test {
	[TestFixture]
	public class ModMonoWorkerTest
	{
		[Test]
		public void TestGetOrCreateApplication ()
		{
			if (!File.Exists ("/chroot"))
				Assert.Fail ("This is not the correct chroot");
			string temp_dir = Path.GetTempPath ();

			string final_vdir;
			string final_pdir;

			ModMonoWorker.GetPhysicalDirectory (temp_dir, temp_dir, out final_vdir, out final_pdir);
		}

		static void AppendSeparator (ref string nested_dir)
		{
			if (!nested_dir.EndsWith (Path.DirectorySeparatorChar.ToString (), StringComparison.Ordinal))
				nested_dir += Path.DirectorySeparatorChar;
		}
	}
}

