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
using System.Xml;
using System.Web.UI.WebControls;
using NUnit.Framework;

namespace NUnit.Extensions.Asp.AspTester
{
	/// <summary>
	/// Tester for System.Web.UI.WebControls.TextBox
	/// </summary>
	public class TextBoxTester : AspControlTester
	{
		/// <summary>
		/// Create the tester and link it to an ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ID of the control to link to.</param>
		/// <param name="container">The control that contains the control to link to</param>
		public TextBoxTester(string aspId, Tester container) : base(aspId, container)
		{
		}

		/// <summary>
		/// The text in the text box.
		/// </summary>
		public string Text {
			set 
			{
				EnterInputValue(GetAttributeValue("name"), value);
			}
			get
			{
				string text;
				if (TextMode == TextBoxMode.MultiLine)
				{
					text = Element.InnerText;       
				}
				else 
				{                                  
					text = GetOptionalAttributeValue("value");
				}

				if (text == null) return "";
				return text;
			}
		}

		/// <summary>
		/// The kind of text box.
		/// </summary>
		public TextBoxMode TextMode 
		{
			get 
			{
				if (TagName == "textarea") return TextBoxMode.MultiLine;
				else 
				{
					Assertion.AssertEquals("tag name", "input", TagName);
					string type = GetAttributeValue("type");
					if (type == "password") return TextBoxMode.Password;
					else 
					{
						Assertion.AssertEquals("type", "text", type);
						return TextBoxMode.SingleLine;
					}
				}
			}
		}

		/// <summary>
		/// Maximum number of characters to display in the text box.  
		/// Returns 0 if there is no max length.
		/// </summary>
		public int MaxLength
		{
			get
			{
				Assertion.Assert("max length is ignored on a TextBox when TextMode is MultiLine", TextMode != TextBoxMode.MultiLine);

				string maxLength = GetOptionalAttributeValue("maxlength");
				if (maxLength == null || maxLength == "") return 0;
				else return int.Parse(maxLength);
			}
		}

	}
}
