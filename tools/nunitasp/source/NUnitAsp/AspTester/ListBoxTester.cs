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
using System.Xml;
using NUnit.Extensions.Asp.AspTester;
using System.Web.UI.WebControls;

namespace NUnit.Extensions.Asp.AspTester
{
	/// <summary>
	/// Tester for System.Web.UI.WebControls.ListBox
	/// </summary>
	public class ListBoxTester : ListControlTester
	{
		/// <summary>
		/// Create the tester and link it to an ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ID of the control to link to.</param>
		/// <param name="container">The control that contains the control to link to</param>
		public ListBoxTester(string aspId, Tester container) : base(aspId, container)
		{
		}

		/// <summary>
		/// Gets the number of rows displayed in the System.Web.UI.WebControls.ListBox control.
		/// </summary>
		public int Rows
		{
			get
			{
				return int.Parse(GetAttributeValue("size"));
			}
		}

		/// <summary>
		/// Gets the selection mode of the System.Web.UI.WebControls.ListBox control.
		/// </summary>
		public ListSelectionMode SelectionMode
		{
			get
			{
				if (Element.Attributes["multiple"] != null)
				{
					return ListSelectionMode.Multiple;
				}
				return ListSelectionMode.Single;
			}
		}

		protected internal override void ChangeItemSelectState(ListItemTester item, bool selected)
		{
			if (!selected || SelectionMode == ListSelectionMode.Single)
			{
				base.ChangeItemSelectState(item, selected);
			}
			else
			{
				EnterInputValue(item.Element, Element.GetAttribute("name"), item.Value);
			}
		}
	}
}
