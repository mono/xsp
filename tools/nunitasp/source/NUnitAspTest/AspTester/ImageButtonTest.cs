#region Copyright (c) 2003, Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2003, Brian Knowles, Jim Shore
' Originally by Clifton F. Vaughn; copyright transferred on nunitasp-devl mailing list, May 2003
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
using NUnit.Extensions.Asp;
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp.Test.AspTester
{
	public class ImageButtonTest : NUnitAspTestCase
	{
		private ImageButtonTester button;
		private ImageButtonTester disabledButton;
		private LabelTester clickResult;

		protected override void SetUp() 
		{
			base.SetUp();
			button = new ImageButtonTester("ImageButton1", CurrentWebForm);
			disabledButton = new ImageButtonTester("Disabled", CurrentWebForm);
			clickResult = new LabelTester("clickResult", CurrentWebForm);

			Browser.GetPage(BaseUrl + "AspTester/ImageButtonTestPage.aspx");
		}

		public void TestClick()
		{
			button.Click(2, 3);
			AssertEquals("result", "2, 3", clickResult.Text);
		}

        [ExpectedException(typeof(ControlDisabledException))]
        public void TestClick_WhenDisabled()
		{
			disabledButton.Click(2, 3);
		}

		public void TestEnabled_True()
		{
			AssertEquals("enabled", true, button.Enabled);
		}

		public void TestEnabled_False()
		{
			AssertEquals("enabled", false, disabledButton.Enabled);
		}

	}
}
