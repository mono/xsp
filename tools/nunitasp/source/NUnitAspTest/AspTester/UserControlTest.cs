#region Copyright (c) 2003 Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2003, Brian Knowles, Jim Shore
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
using NUnit.Extensions.Asp;
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp.Test.AspTester
{
	public class UserControlTest : NUnitAspTestCase
	{
		private LabelTester label;
		private ButtonTester button;
		private LinkButtonTester linkButton;

		private LabelTester clickResult;

		protected override void SetUp() 
		{
			base.SetUp();

			UserControlTester userControl = new UserControlTester("userControl", CurrentWebForm);

			label = new LabelTester("label", userControl);
			button = new ButtonTester("button", userControl);
			linkButton = new LinkButtonTester("linkButton", userControl);

			clickResult = new LabelTester("clickResult", CurrentWebForm);

			Browser.GetPage(BaseUrl + "AspTester/UserControlTestPage.aspx");
		}


		public void TestNestedLabelText()
		{
			AssertEquals("Label", label.Text);
		}

		public void TestNestedButtonClick()
		{
			button.Click();
			AssertEquals("result", "Clicked", clickResult.Text);
		}

		public void TestNestedLinkButtonClick()
		{
			linkButton.Click();
			AssertEquals("result", "Clicked", clickResult.Text);
		}
	}
}
