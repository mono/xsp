#region Copyright (c) 2002, 2003, Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2002, 2003, Brian Knowles, Jim Shore
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

namespace NUnit.Extensions.Asp.AspTester
{
	/// <summary>
	/// Tester for System.Web.UI.WebControls.RadioButton
	/// </summary>
	public class RadioButtonTester : CheckBoxTester
	{
		/// <summary>
		/// Create the tester and link it to an ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ID of the control to link to.</param>
		/// <param name="container">The control that contains the control to link to</param>
		public RadioButtonTester(string aspId, Tester container) : base(aspId, container)
		{
		}

		/// <summary>
		/// The name of the group that the radio button is part of.
		/// </summary>
		public string GroupName
		{
			get
			{
				string mangledGroupName = GetAttributeValue("name");
				return mangledGroupName.Substring(mangledGroupName.LastIndexOf(":") + 1);
			}
		}

		/// <summary>
		/// True if the radio button is checked, false if not.
		/// </summary>
		public override bool Checked
		{
			get
			{
				return base.Checked;
			}
			set
			{
				if (value == true) 
				{
					UncheckWholeGroup();
					base.Checked = value;
				}
				else
				{
					throw new CannotUncheckException();
				}
			}
		}

		private void UncheckWholeGroup()
		{
			string name = GetAttributeValue("name");
			string groupExpr = string.Format("//form//input[@type='radio'][@name='{0}']", name);

			foreach (XmlElement radio in Element.SelectNodes(groupExpr))
			{
				RemoveInputValue(radio, name);
			}
		}	

		/// <summary>
		/// Test attempted to set RadioButton's Checked property to false.
		/// But radio buttons cannot be unchecked directly, check another 
		/// radio button in the same group instead.
		/// </summary>
		public class CannotUncheckException : InvalidOperationException
		{
			public CannotUncheckException() : 
				base("Cannot uncheck radio button, check another one in the same group instead.")
			{
			}
		}
	}
}
