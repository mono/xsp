#region Copyright (c) 2003, Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2003, Brian Knowles, Jim Shore
' Originally by Andrew Enfield; copyright transferred on nunitasp-devl mailing list, May 2003
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

namespace NUnit.Extensions.Asp.HtmlTester
{
	/// <summary>
	/// Tester for the System.Web.UI.HtmlControls.HtmlInputCheckBox control.
	/// </summary>
	public class HtmlInputCheckBoxTester : HtmlControlTester
	{
		private bool runAtServer;

		/// <summary>
		/// Create the tester and connect it to the specified ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ASP ID of the control to be tested (i.e., the control to which this tester should be linked).</param>
		/// <param name="container">The control that contains the control to be tested.</param>
		/// <param name="runAtServer">True if the control to be tested has the 'runAtServer="true"' attribute.</param>
		public HtmlInputCheckBoxTester(String aspId, Tester container, bool runAtServer) : base(aspId, container) 
		{
			this.runAtServer = runAtServer;
		}

		public bool Checked 
		{
			get 
			{
				return GetOptionalAttributeValue("checked") != null;
			}
			set 
			{
				if (value == true) 
				{
					string checkBoxValue = GetOptionalAttributeValue("value");
					if (checkBoxValue == null) checkBoxValue = "on";
					EnterInputValue(GetAttributeValue("name"), checkBoxValue);
				}
				else
				{
					RemoveInputValue(GetAttributeValue("name"));
				}
			}
		}

		public override string HtmlId
		{
			get
			{
				if (runAtServer) return base.HtmlId;
				else return AspId;
			}
		}
	}
}
