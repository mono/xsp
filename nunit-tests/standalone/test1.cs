//
// test1
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
	public class Test1 : WebFormTestCase
	{
		LabelTester label;
		ButtonTester button;

		protected override void SetUp() 
		{
		}
		
		[Test]
		public void Bug51682 ()
		{
			// bug51682 - Label ID and ClientID
			Browser.GetPage ("http://127.0.0.1:8080/test1.aspx");
			label = new LabelTester ("Label1", CurrentWebForm);
			AssertEquals ("#01", "The Page.ClientID is:Hello", label.Text);
		}

		[Test]
		public void Bug52334 ()
		{
			// bug52334 - columns not tracking view state
			Browser.GetPage ("http://127.0.0.1:8080/test2.aspx");
			button = new ButtonTester ("Button1", CurrentWebForm);
			button.Click ();
			label = new LabelTester ("Label1", CurrentWebForm);
			AssertEquals ("#01", "field:test", label.Text);
		}

		[Test]
		public void Bug50154 ()
		{
			// Unexpected OnClick raised for button
			Browser.GetPage ("http://127.0.0.1:8080/test3.aspx");
			label = new LabelTester ("Label1", CurrentWebForm);
			AssertEquals ("#01", "", label.Text);
		}
	}
}
