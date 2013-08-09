using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace Mono.WebServer.Test {
	[TestFixture]
	public class CompatArraySegmentTest
	{
		[Test]
		public void TestCase ()
		{
			var test = new CompatArraySegment<int> (new int[1]);
			IList<int> ilist = test;

			test [0] = -3;
			Assert.AreEqual (-3, ilist [0]);
		}
	}
}

