using System;
using NUnit.Framework;

namespace NUnit.Extensions.Asp.Test
{
	[TestFixture]
	public class SetupTest : NUnitAspTestCase
	{
		[SetUp]
		public void MySetUp()
		{
		}

		[TearDown]
		public void MyTearDown()
		{
		}

		[Test]
		public void TestSetupAttributeErrorMessage()
		{
			try
			{
				Browser.GetPage(BaseUrl);
			}
			catch (NullReferenceException)
			{
				Fail("Should either succeed or warn user when [SetUp] attribute is used");
			}
			catch (InvalidOperationException)
			{
				// okay to get this exception... for now
			}
		}
	}
}
