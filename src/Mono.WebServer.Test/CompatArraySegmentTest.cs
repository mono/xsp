using Mono.WebServer.FastCgi.Compatibility;
using NUnit.Framework;

namespace Mono.WebServer.Test {
	[TestFixture]
	public class CompatArraySegmentTest
	{
		[Test]
		public void TestCase ()
		{
			var test = new CompatArraySegment<int> (new int[1]);

			test [0] = -3;
			Assert.AreEqual (-3, test [0]);
		}
	}
}

