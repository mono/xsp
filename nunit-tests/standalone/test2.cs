//
// test2: bug52334 - columns not tracking view state
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
//


using System;
using NUnit.Framework;
using NUnit.Extensions.Asp;
using NUnit.Extensions.Asp.AspTester;

namespace Mono.ASPNET
{
	[TestFixture]
	public class Test2 : WebFormTestCase
	{
		LabelTester label;

		protected override void SetUp() 
		{
			Browser.GetPage ("http://127.0.0.1:8080/test2.aspx");
			label = new LabelTester ("Label1", CurrentWebForm);
		}
		
		[Test]
		public void Bug52334 ()
		{
			AssertEquals ("#01", "field:test", label.Text);
		}
	}
}
