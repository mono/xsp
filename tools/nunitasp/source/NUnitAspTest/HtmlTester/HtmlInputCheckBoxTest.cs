#region Copyright (c) 2003, Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2003, Brian Knowles, Jim Shore
' Originally by Andrew Enfield; copyright transferred on nunitasp-devl mailing list, May 2003
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
'
'*******************************************************************************************************************/
#endregion

using System;
using NUnit.Framework;
using NUnit.Extensions.Asp.HtmlTester;
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp.Test.HtmlTester
{

	public class HtmlInputCheckBoxTest : NUnitAspTestCase
	{
		private HtmlInputCheckBoxTester checkNotCheckedServer;
		private HtmlInputCheckBoxTester checkCheckedServer;
		private HtmlInputCheckBoxTester checkNotCheckedNotServer;
		private HtmlInputCheckBoxTester checkCheckedNotServer;
		private HtmlInputCheckBoxTester checkVaryServer;
		private HtmlInputCheckBoxTester checkVaryNotServer;
		private HtmlInputCheckBoxTester checkDisabled;

		private LinkButtonTester submit;

		protected override void SetUp() 
		{
			base.SetUp();

			checkNotCheckedServer = new HtmlInputCheckBoxTester("checkNotCheckedServer", CurrentWebForm, true);
			checkCheckedServer = new HtmlInputCheckBoxTester("checkCheckedServer", CurrentWebForm, true);
			checkNotCheckedNotServer = new HtmlInputCheckBoxTester("checkNotCheckedServer", CurrentWebForm, false);
			checkCheckedNotServer = new HtmlInputCheckBoxTester("checkCheckedNotServer", CurrentWebForm, false);
			checkVaryServer = new HtmlInputCheckBoxTester("checkVaryServer", CurrentWebForm, true);
			checkVaryNotServer = new HtmlInputCheckBoxTester("checkVaryNotServer", CurrentWebForm, false);
			checkDisabled = new HtmlInputCheckBoxTester("checkDisabled", CurrentWebForm, false);

			submit = new LinkButtonTester("submit", CurrentWebForm);

			Browser.GetPage(BaseUrl + "HtmlTester/HtmlInputCheckBoxTestPage.aspx");
		}

		public void TestCheckBoxSetAndNotSet() 
		{
			Assert("checkNotCheckedServer should not be checked", !checkNotCheckedServer.Checked);
			Assert("checkCheckedServer should be checked", checkCheckedServer.Checked);
			Assert("checkNotCheckedNotServer should not be checked", !checkNotCheckedNotServer.Checked);
			Assert("checkCheckedNotServer should be checked", checkCheckedNotServer.Checked);
		}

		public void TestCheckVary() 
		{
			Assert("checkVaryServer should not be checked", !checkVaryServer.Checked);
			checkVaryServer.Checked = true;
			Assert("checkVaryServer still shouldn't be checked - not submitted yet", !checkVaryServer.Checked);
			submit.Click();
			Assert("checkVaryServer should be checked since it's been submitted", checkVaryServer.Checked);
			checkVaryServer.Checked = false;
			Assert("checkVaryServer should still be checked - not submitted yet", checkVaryServer.Checked);
			submit.Click();
			Assert("checkVaryServer should not be checked since it's been submitted", !checkVaryServer.Checked);			
		}

		public void TestCheckVaryNotServer() 
		{
			Assert("checkVaryNotServer should not be checked", !checkVaryNotServer.Checked);
			checkVaryNotServer.Checked = true;
			Assert("checkVaryNotServer still shouldn't be checked - not submitted (but won't change anyway)", !checkVaryNotServer.Checked);
			submit.Click();
			Assert("checkVaryNotServer still shouldn't be checked - plain (non ASP.NET) controls don't automatically remember state", !checkVaryNotServer.Checked);
		}

        [ExpectedException(typeof(ControlDisabledException))]
        public void TestCheck_WhenDisabled()
		{
			checkDisabled.Checked = true;
		}

		public void TestDisabled_True()
		{
			AssertEquals("enabled", true, checkDisabled.Disabled);
		}

		public void TestDisabled_False()
		{
			AssertEquals("enabled", false, checkVaryServer.Disabled);
		}
	}
}
