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
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web.UI.WebControls;

namespace NUnit.Extensions.Asp.AspTester
{
	/// <summary>
	/// Tester for System.Web.UI.WebControls.ListItemCollection
	/// </summary>
	public class ListItemCollectionTester : ReadOnlyCollectionBase
	{
		internal ListItemCollectionTester(ListControlTester control, XmlNodeList optionList)
		{
			foreach (XmlElement option in optionList)
			{
				InnerList.Add(new ListItemTester(control, option));
			}
		}

		/// <summary>
		/// Gets ListItemTesters contained in this collection by zero-based index.
		/// </summary>
		public ListItemTester this[int index]
		{
			get
			{
				return (ListItemTester)InnerList[index];
			}
		}

		/// <summary>
		/// Determines whether the collection contains the specified item.
		/// </summary>
		/// <param name="item">A System.Web.UI.WebControls.ListItem to search for in the collection.</param>
		/// <returns>true if the collection contains the specified item; otherwise, false.</returns>
		public bool Contains(ListItemTester item)
		{
			return InnerList.Contains(item);
		}

		/// <summary>
		/// Searches the collection for a ListItemTester with whose Text property contains the specified text.
		/// </summary>
		/// <param name="text">The text to search for.</param>
		/// <returns>A ListItemTester that contains the text specified by the text parameter.</returns>
		public ListItemTester FindByText(string text)
		{
			foreach (ListItemTester item in this)
			{
				if (item.Text == text) return item;
			}
			return null;
		}

		/// <summary>
		/// Searches the collection for a ListItemTester with whose Value property contains the specified value.
		/// </summary>
		/// <param name="value"> The value to search for.</param>
		/// <returns>A ListItemTester that contains the value specified by the value parameter.</returns>
		public ListItemTester FindByValue(string value)
		{
			foreach (ListItemTester item in this)
			{
				if (item.Value == value) return item;
			}
			return null;
		}
	}
}
