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
	public abstract class ListControlTest : NUnitAspTestCase
	{
		private ListControlTester list;
		private ListControlTester emptyList;
		private ListControlTester disabledList;
		private LinkButtonTester submit;
		private LinkButtonTester clearSelection;
		private CheckBoxTester autoPostBack;
		private LabelTester indexChanged;

		protected ListControlTester List
		{
			get
			{
				return list;
			}
		}

		protected LinkButtonTester Submit
		{
			get
			{
				return submit;
			}
		}


		protected abstract ListControlTester CreateListControl(string aspId, Tester container);

		protected override void SetUp()
		{
			base.SetUp();
			list = CreateListControl("list", CurrentWebForm);
			emptyList = CreateListControl("emptyList", CurrentWebForm);
			disabledList = CreateListControl("disabledList", CurrentWebForm);
			submit = new LinkButtonTester("submit", CurrentWebForm);
			clearSelection = new LinkButtonTester("clearSelection", CurrentWebForm);
			autoPostBack = new CheckBoxTester("auto", CurrentWebForm);
			indexChanged = new LabelTester("indexChanged", CurrentWebForm);
		}


		protected void DoTestSetItemsSelected_WhenSingleSelect()
		{
			AssertEquals(1, List.SelectedIndex);

			List.Items[0].Selected = true;
			List.Items[2].Selected = true;
			Submit.Click();

			AssertEquals("item #0 selected", false, List.Items[0].Selected);
			AssertEquals("item #1 selected", false, List.Items[1].Selected);
			AssertEquals("item #2 selected", true, List.Items[2].Selected);
		}

		protected void DoTestSelectionPreserved_WhenSingle()
		{
			DoTestSetItemsSelected_WhenSingleSelect();
			Submit.Click();

			AssertEquals("item #0 selected", false, List.Items[0].Selected);
			AssertEquals("item #1 selected", false, List.Items[1].Selected);
			AssertEquals("item #2 selected", true, List.Items[2].Selected);
		}

		[Test]
		public void TestEnabled_True()
		{
			AssertEquals("enabled", true, List.Enabled);
		}

		[Test]
		public void TestEnabled_False()
		{
			AssertEquals("enabled", false, disabledList.Enabled);
		}

		[Test]
		public void TestSelectedIndex()
		{
			AssertEquals("selected index", 1, List.SelectedIndex);
		}

		[Test]
		public void TestSelectedItem()
		{
			ListItemTester item = List.SelectedItem;
			AssertEquals("text", "two", item.Text);
			AssertEquals("value", "2", item.Value);

			Assert("selected item's selected property is false",
				item.Selected);
			Assert("unselected item's selected property is true",
				!List.Items[0].Selected);
		}

		[Test]
		public void TestItems()
		{
			ListItemCollection expectedItems = new ListItemCollection();
			expectedItems.Add(new ListItem("one", "1"));
			expectedItems.Add(new ListItem("two", "2"));
			expectedItems.Add(new ListItem("three", "3"));

			ListItemCollectionTester actualItems = List.Items;
			AssertEquals("# of items", 3, actualItems.Count);
			for (int i = 0; i < actualItems.Count; i++)
			{
				AssertEquals("Item text #" + i, expectedItems[i].Text, actualItems[i].Text);
				AssertEquals("Item value #" + i, expectedItems[i].Value, actualItems[i].Value);
			}
		}

		[Test]
		public void TestSetSelectedIndex()
		{
			AssertEquals(1, List.SelectedIndex);
			List.SelectedIndex = 2;
			AssertEquals("No", indexChanged.Text);
			AssertEquals(1, List.SelectedIndex);
			Submit.Click();
			AssertEquals("Yes", indexChanged.Text);
			AssertEquals(2, List.SelectedIndex);
		}

		[Test]
		[ExpectedException(typeof(ControlDisabledException))]
		public void TestSetSelectedIndex_WhenDisabled()
		{
			disabledList.SelectedIndex = 1;
		}

		[Test]
		public void TestImmediatePostBack()
		{
			autoPostBack.Checked = true;
			Submit.Click();
			AssertEquals("selected index (before modification)", 1, List.SelectedIndex);
			List.SelectedIndex = 2;
			AssertEquals("Yes", indexChanged.Text);
			AssertEquals("selected index (after modification)", 2, List.SelectedIndex);
		}

		[Test]
		public void TestSetSelectedIndexOutOfRange()
		{
			try
			{
				List.SelectedIndex = 5;
				Fail("Expected IllegalInputException");
			}
			catch (DropDownListTester.IllegalInputException)
			{
				// expected
			}
		}

		[Test]
		public void TestServerSideClearSelection()
		{
			try
			{
				clearSelection.Click();
				int unused = List.SelectedIndex;
				Fail("Expected NoSelectionException");
			}
			catch (DropDownListTester.NoSelectionException)
			{
				// expected
			}
		}

		[Test]
		public void TestSelectedIndex_WhenEmptyList()
		{
			try
			{
				int unused = emptyList.SelectedIndex;
				Fail("Expected NoSelectionException");
			}
			catch (DropDownListTester.NoSelectionException)
			{
				// expected
			}
		}

		[Test]
		public void TestSelectedItem_WhenEmptyList()
		{
			try
			{
				ListItemTester unused = emptyList.SelectedItem;
				Fail("Expected NoSelectionException");
			}
			catch (DropDownListTester.NoSelectionException)
			{
				// expected
			}
		}

		[Test]
		public void TestItems_WhenEmptyList()
		{
			AssertEquals("# of items", 0, emptyList.Items.Count);
		}

		[Test]
		public void TestSetSelectedIndex_WhenEmptyList()
		{
			try
			{
				emptyList.SelectedIndex = 0;
				Fail("Expected IllegalInputException");
			}
			catch (DropDownListTester.IllegalInputException)
			{
				// expected
			}
		}

		[Test]
		public void TestSelectedIndexChangedEvent_WhenItemAddedToList()
		{
			ButtonTester addItem = new ButtonTester("add", CurrentWebForm);
			
			autoPostBack.Checked = true;
			Submit.Click();
			addItem.Click();
			AssertEquals("No", indexChanged.Text);
		}
	}
}
