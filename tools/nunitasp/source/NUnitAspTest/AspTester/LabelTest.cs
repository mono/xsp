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

using System;
using NUnit.Extensions.Asp.AspTester;
using NUnit.Framework;

namespace NUnit.Extensions.Asp.Test.AspTester
{

	public class LabelTest : NUnitAspTestCase
	{
		protected override void SetUp()
		{
			base.SetUp();
			Browser.GetPage(BaseUrl + "/AspTester/LabelTestPage.aspx");
		}

		public void TestText() 
		{
			LabelTester textLabel = new LabelTester("textLabel", CurrentWebForm);
			AssertEquals("text", "foo", textLabel.Text);
		}

		public void TestSpace()
		{
			LabelTester spaceLabel = new LabelTester("spaceLabel", CurrentWebForm);
			AssertEquals("space", "foo ", spaceLabel.Text);
		}

		public void TestFormatted()
		{
			LabelTester formattedLabel = new LabelTester("formattedLabel", CurrentWebForm);
			AssertEquals("formatted", "a <i>HTML</i> tag", formattedLabel.Text);
		}

		public void TestNested()
		{
			LabelTester outerLabel = new LabelTester("outerLabel", CurrentWebForm);
			LabelTester innerLabel = new LabelTester("innerLabel", outerLabel);
			AssertEquals("inner", "inner", innerLabel.Text);
		}
	}
}
