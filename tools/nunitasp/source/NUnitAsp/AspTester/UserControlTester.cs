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

namespace NUnit.Extensions.Asp.AspTester
{
	/// <summary>
	/// Tester for System.Web.UI.UserControl
	/// </summary>
	public class UserControlTester : AspControlTester
	{
		private string aspId;
		private Tester container;

		/// <summary>
		/// Create the tester and link it to an ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ID of the control to link to.</param>
		/// <param name="container">The control that contains the control to link to</param>
		public UserControlTester(string aspId, Tester container) : base(aspId, container)
		{
			this.aspId = aspId;
			this.container = container;
		}

		protected internal override string GetChildElementHtmlId(string aspId)
		{
			return HtmlId + "_" + aspId;
		}

		protected override bool IsDisabled
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Visibility of user controls cannot be determined.  This method always throws an
		/// exception.
		/// </summary>
		public override bool Visible
		{
			get
			{
				throw new VisibilityException(this.GetType().Name);
			}
		}

		/// <summary>
		/// The test tried to check the visibility of a user control.  There's no way to 
		/// directly check user control visibility because they don't generate any HTML of
		/// their own.  Change the test to check the visibility of a control inside the user
		/// control instead.
		/// </summary>
		private class VisibilityException : ApplicationException
		{
			internal VisibilityException(string className) : base(className + "s cannot be tested for visibility because they don't directly generate HTML tags")
			{
			}
		}
	}
}
