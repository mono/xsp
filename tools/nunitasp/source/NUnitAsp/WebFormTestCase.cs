#region Copyright (c) 2002, 2003 by Brian Knowles and Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2002, 2003 by Brian Knowles and Jim Shore
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
'******************************************************************************************************************/
#endregion

using System;
using NUnit.Framework;
using System.Xml;
using NUnit.Extensions.Asp.AspTester;

namespace NUnit.Extensions.Asp 
{ 
	/// <summary>
	/// Base class for NUnitAsp test fixtures.  Extend this class to use NUnitAsp.
	/// </summary>
	[TestFixture]
	public abstract class WebFormTestCase : WebAssertion
	{
		private HttpClient browser;
		private WebForm form;

		public WebFormTestCase()
		{
		}

		/// <summary>
		/// Do not call.  For use by NUnit only.
		/// </summary>
		[SetUp]
		public void BaseSetUp() 
		{
			browser = new HttpClient();
			form = new WebForm(browser);
			SetUp();
		}

		/// <summary>
		/// Executed before each test method is run.  Override in subclasses to do subclass
		/// set up.  NOTE: [SetUp] attribute cannot be used in subclasses because it is already
		/// in use.
		/// </summary>
		protected virtual void SetUp()
		{
		}

		/// <summary>
		/// Do not call.  For use by NUnit only.
		/// </summary>
		[TearDown]
		protected void BaseTearDown()
		{
			TearDown();
		}

		/// <summary>
		/// Executed after each test method is run.  Override in subclasses to do subclass
		/// clean up.  NOTE: [TearDown] attribute cannot be used in subclasses because it is
		/// already in use.
		/// </summary>
		protected virtual void TearDown()
		{
		}

		/// <summary>
		/// The web form currently loaded by the browser.
		/// </summary>
		protected WebForm CurrentWebForm
		{
			get 
			{
				CheckSetUp();
				return form;
			}
		}

		/// <summary>
		/// The web browser.
		/// </summary>
		protected HttpClient Browser 
		{
			get 
			{
				CheckSetUp();
				return browser;
			}
		}

		private void CheckSetUp()
		{
			if (form == null || browser == null) 
			{
				throw new InvalidOperationException("A required setup method in WebFormTestCase was not called.  This is probably because you used the [SetUp] attribute in a subclass of WebFormTestCase.  That is not supported.  Override the SetUp() method instead.");
			}
		}
	}
}