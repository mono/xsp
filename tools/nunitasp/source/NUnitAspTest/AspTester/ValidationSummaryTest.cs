/********************************************************************************************************************
'
' Copyright (c) 2002, Brian Knowles, Jim Shore
' Originally written by David Paxson.  Copyright assigned to Brian Knowles and Jim Shore
' on the nunitasp-devl@lists.sourceforge.net mailing list on 28 Aug 2002.
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

using System;
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp.Test.AspTester
{
	public class ValidationSummaryTest : NUnitAspTestCase
	{
		private TextBoxTester textBox;
		private ValidationSummaryTester bulletedSummary;
		private ValidationSummaryTester listSummary;
		private ButtonTester button;

		protected override void SetUp()
		{
			base.SetUp();
			Browser.GetPage(BaseUrl + "AspTester/ValidationSummaryTestPage.aspx");

			textBox = new TextBoxTester("textbox", CurrentWebForm);
			bulletedSummary = new ValidationSummaryTester("bulletedSummary", CurrentWebForm);
			listSummary = new ValidationSummaryTester("listSummary", CurrentWebForm);
			button = new ButtonTester("submit", CurrentWebForm);
		}

		public void TestPageLayout()
		{
			AssertEquals("", textBox.Text);

			AssertVisibility(bulletedSummary, false);
			AssertVisibility(listSummary, false);
			AssertVisibility(button, true);
		}

		public void TestBulletedSummary()
		{
			AssertCorrectMessages(bulletedSummary);
		}

		public void TestListSummary()
		{
			AssertCorrectMessages(listSummary);
		}


		private void AssertCorrectMessages(ValidationSummaryTester summary)
		{
			AssertEquals("", textBox.Text);
			button.Click();
			AssertVisibility(summary, true);

			string[] expected = new string[] {"First message", "Second message", "Third message"};
			AssertEquals("messages", expected, summary.Messages);
		}
	}
}
