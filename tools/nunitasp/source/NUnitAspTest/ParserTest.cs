#region Copyright (c) 2002, Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2002, Brian Knowles, Jim Shore
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
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp.Test
{
	[TestFixture]
	public class ParserTest : NUnitAspTestCase
	{
		[Test]
		public void TestMalformedDocument()
		{
			CheckBoxTester control = new CheckBoxTester("control", CurrentWebForm);
			Browser.GetPage(BaseUrl + "MalformedTestPage.aspx");
			AssertVisibility(control, true);
		}

		[Test]
		public void TestParseFormVariables_Checkbox()
		{
			Browser.GetPage(BaseUrl + "FormVariableTestPage.aspx");
			new ButtonTester("submit", CurrentWebForm).Click();
			Assert("should be checked", new CheckBoxTester("checkbox", CurrentWebForm).Checked);
		}

		[Test]
		[ExpectedException(typeof(DoctypeDtdException))]
		public void TestOldFashionedAndTotallyIncorrectDoctype()
		{
			Browser.GetPage(BaseUrl + "LocalhostDoctype.aspx");
			AssertVisibility(new LabelTester("label", CurrentWebForm), true);
		}
	}
}
