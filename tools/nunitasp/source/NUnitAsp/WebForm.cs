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
using NUnit.Framework;

namespace NUnit.Extensions.Asp
{

	/// <summary>
	/// A tester for an ASP.NET form.  Most of the methods in this class aren't meant to
	/// be called by third parties.
	/// 
	/// The API for this class will change in future releases.  
	/// </summary>
	public class WebForm : Tester
	{
		HttpClient browser;

		public WebForm(HttpClient browser)
		{
			this.browser = browser;
		}

		protected internal override HttpClient Browser
		{
			get
			{
				return browser;
			}
		}

		public override bool HasChildElement(string htmlId)
		{
			return (GetElementInternal(htmlId) != null);
		}

		protected internal override XmlElement GetChildElement(string htmlId)
		{
			XmlElement element = GetElementInternal(htmlId);
			if (element == null) throw new ElementNotVisibleException("Couldn't find " + htmlId + " on " + Description);
			return element;
		}

		protected internal override string GetChildElementHtmlId(string aspId)
		{
			return aspId;
		}

		private XmlElement GetElementInternal(string htmlId)
		{
			return browser.CurrentPage.GetElementById(htmlId);
		}

		protected internal override void EnterInputValue(XmlElement owner, string name, string value)
		{
			browser.SetFormVariable(owner, name, value);
		}

		protected internal override void RemoveInputValue(XmlElement owner, string name)
		{
			browser.ClearFormVariable(owner, name);
		}

		protected internal override void Submit()
		{
			browser.SubmitForm(this);
		}

		internal string Action
		{
			get
			{
				return GetAttributeValue("action");
			}
		}

		internal string Method
		{
			get
			{
				return GetAttributeValue("method");
			}
		}

		public override string Description
		{
			get
			{
				return "web form '" + AspId + "'";
			}
		}

		private XmlElement Element
		{
			get
			{
				XmlNodeList formNodes = browser.CurrentPage.GetElementsByTagName("form");
				Assertion.AssertEquals("page form elements", 1, formNodes.Count);
				return (XmlElement)formNodes[0];
			}
		}

		private string GetAttributeValue(string name) 
		{
			XmlAttribute attribute = Element.Attributes[name];
			if (attribute == null) throw new AttributeMissingException(name, Description);
			return attribute.Value;
		}

		public string AspId
		{
			get
			{
				return GetAttributeValue("id");
			}
		}

		private class ElementNotVisibleException : ApplicationException
		{
			internal ElementNotVisibleException(string message) : base(message)
			{
			}
		}
	}
}
