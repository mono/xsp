#region Copyright (c) 2003, Brian Knowles, Jim Shore
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
using System.Web.UI.WebControls;

using NUnit.Framework;
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp.Test.AspTester
{
	[TestFixture]
	public class ListBoxTest : ListControlTest
	{
		private CheckBoxTester multiSelect;

		protected new ListBoxTester List
		{
			get
			{
				return (ListBoxTester)base.List;
			}
		}

		protected override void SetUp()
		{
			base.SetUp();
			multiSelect = new CheckBoxTester("multi", CurrentWebForm);

			Browser.GetPage(BaseUrl + "/AspTester/ListBoxTestPage.aspx");
		}

		protected override ListControlTester CreateListControl(string aspId, Tester container)
		{
			return new ListBoxTester(aspId, container);
		}

		[Test]
		public void TestRows()
		{
			AssertEquals("# of rows", 4, List.Rows);
		}

		[Test]
		public void TestSelectionMode()
		{
			AssertEquals("Selection mode", ListSelectionMode.Single, List.SelectionMode);

			multiSelect.Checked = true;
			Submit.Click();
			AssertEquals("Selection mode", ListSelectionMode.Multiple, List.SelectionMode);
		}

		[Test]
		public void TestSetItemsSelected_WhenSingleSelect()
		{
			AssertEquals(ListSelectionMode.Single, List.SelectionMode);
			DoTestSetItemsSelected_WhenSingleSelect();
		}

		[Test]
		public void TestSelectionPreserved_WhenSingle()
		{
			AssertEquals(ListSelectionMode.Single, List.SelectionMode);
			DoTestSelectionPreserved_WhenSingle();
		}

		[Test]
		public void TestSetItemsSelected_WhenMultipleSelect()
		{
			multiSelect.Checked = true;
			Submit.Click();

			AssertEquals(ListSelectionMode.Multiple, List.SelectionMode);
			AssertEquals(1, List.SelectedIndex);

			List.Items[0].Selected = true;
			List.Items[2].Selected = true;
			Submit.Click();

			AssertEquals("item #0 selected", true, List.Items[0].Selected);
			AssertEquals("item #1 selected", true, List.Items[1].Selected);
			AssertEquals("item #2 selected", true, List.Items[2].Selected);
		}

		[Test]
		public void TestSelectionPreserved_WhenMultiple()
		{
			TestSetItemsSelected_WhenMultipleSelect();
			Submit.Click();

			AssertEquals("item #0 selected", true, List.Items[0].Selected);
			AssertEquals("item #1 selected", true, List.Items[1].Selected);
			AssertEquals("item #2 selected", true, List.Items[2].Selected);
		}
	}
}
