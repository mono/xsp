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
	/// Tester for System.Web.UI.WebControls.ListItem
	/// </summary>
	public class ListItemTester
	{
		private readonly ListControlTester owner;
		internal readonly XmlElement Element;

		internal ListItemTester(ListControlTester owner, XmlElement element)
		{
			this.owner = owner;
			Element = element;
		}

		/// <summary>
		/// Gets the text displayed in a list control for the item represented by the ListItemTester.
		/// </summary>
		public string Text
		{
			get
			{
				return Element.InnerText;
			}
		}

		/// <summary>
		/// Gets the value associated with the ListItemTester.
		/// </summary>
		public string Value
		{
			get
			{
				XmlAttribute valueAttribute = Element.Attributes["value"];

				if (valueAttribute == null)
				{
					return Text;
				}

				return valueAttribute.Value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the item is selected.
		/// </summary>
		public bool Selected
		{
			get
			{
				return Element.Attributes["selected"] != null;
			}
			set
			{
				owner.ChangeItemSelectState(this, value);
			}
		}


		/// <summary>
		/// Returns a string that represents the current ListItemTester.
		/// </summary>
		/// <returns>ListItemTester's Text property.</returns>
		public override string ToString()
		{
			return Text;
		}
	}
}
