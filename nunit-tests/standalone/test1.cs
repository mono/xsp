//
// test1: bug51682 - Label ID and ClientID
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

		protected override void SetUp() 
		{
			Browser.GetPage ("http://127.0.0.1:8080/test1.aspx");
			label = new LabelTester ("Label1", CurrentWebForm);
		}
		
		[Test]
		public void Bug51682 ()
		{
			AssertEquals ("#01", "The Page.ClientID is:Hello", label.Text);
		}
	}
}
