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
using NUnit.Framework;
using System.Xml;
using System.Text.RegularExpressions;

namespace NUnit.Extensions.Asp
{
	/// <summary>
	/// Base class for all tag-based controls.  Extend this class if you're creating a
	/// custom tester.
	/// 
	/// The API for this class will change in future releases.
	/// </summary>
	public abstract class ControlTester : Tester
	{
		private string aspId;
		private Tester container;

		internal ControlTester(string aspId, Tester container)
		{
			this.aspId = aspId;
			this.container = container;
		}

		protected ControlTester()
		{
		}

		public virtual bool Visible
		{
			get
			{
				return container.HasChildElement(HtmlId);
			}
		}

		/// <summary>
		/// Returns null if attribute not found
		/// </summary>
		protected string GetOptionalAttributeValue(string name)
		{
			XmlAttribute attrib = Element.Attributes[name];
			if (attrib == null) return null;
			return attrib.Value;
		}

		protected string GetAttributeValue(string name) 
		{
			string attributeValue = GetOptionalAttributeValue(name);
			string message = string.Format("Expected attribute '{0}' in {1}", name, HtmlIdAndDescription);
			Assertion.AssertNotNull(message, attributeValue);
			return attributeValue;
		}

		protected string TagName
		{
			get
			{
				return Element.Name;
			}
		}

		protected internal override XmlElement GetChildElement(string htmlId)
		{
			return container.GetChildElement(htmlId);
		}

		public override bool HasChildElement(string htmlId)
		{
			return container.HasChildElement(htmlId);
		}

		protected internal override string GetChildElementHtmlId(string aspId)
		{
			return container.GetChildElementHtmlId(aspId);
		}

		protected internal virtual XmlElement Element
		{
			get 
			{
				return container.GetChildElement(HtmlId);
			}
		}

		protected internal override HttpClient Browser
		{
			get
			{
				return container.Browser;
			}
		}

		public string HtmlIdAndDescription
		{
			get
			{
				return string.Format("{0} ({1})", HtmlId, Description);
			}
		}

		public override string Description 
		{
			get 
			{
				string controlType = this.GetType().Name;
				return string.Format("{0} '{1}' in {2}", controlType, aspId, container.Description);
			}
		}

		public string AspId
		{
			get
			{
				return aspId;
			}
		}

		public virtual string HtmlId
		{
			get
			{
				return container.GetChildElementHtmlId(aspId);
			}
		}

		protected virtual bool IsDisabled
		{
			get
			{
				return Element.Attributes["disabled"] != null;
			}
		}

		private void EnsureEnabled()
		{
			if (IsDisabled)
			{
				throw new ControlDisabledException(this);
			}
		}

		protected internal override void EnterInputValue(XmlElement owner, string name, string value)
		{
			EnsureEnabled();
			container.EnterInputValue(owner, name, value);
		}

		protected internal override void RemoveInputValue(XmlElement owner, string name)
		{
			EnsureEnabled();
			container.RemoveInputValue(owner, name);
		}

		protected void EnterInputValue(string name, string value)
		{
			EnterInputValue(Element, name, value);
		}

		protected void RemoveInputValue(string name)
		{
			RemoveInputValue(Element, name);
		}

		protected internal override void Submit()
		{
			container.Submit();
		}

		/// <summary>
		/// Works properly even if candidatePostBackScript is null.
		/// </summary>
		protected void OptionalPostBack(string candidatePostBackScript)
		{
			if (IsPostBack(candidatePostBackScript))
			{
				PostBack(candidatePostBackScript);
			}
		}

		private bool IsPostBack(string candidate)
		{
			return (candidate != null) && (candidate.StartsWith("__doPostBack"));
		}

		private void SetInputHiddenValue(string name, string value)
		{
			string expression = string.Format("//form//input[@type='hidden'][@name='{0}']", name);
			container.EnterInputValue((XmlElement)Element.SelectSingleNode(expression), name, value);
		}

		protected void PostBack(string postBackScript)
		{
			string postBackPattern = @"__doPostBack\('(?<target>.*?)','(?<argument>.*?)'\)";

			Match match = Regex.Match(postBackScript, postBackPattern, RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				throw new ParseException("'" + postBackScript + "' doesn't match expected pattern for postback in " + HtmlIdAndDescription);
			}

			string target = match.Groups["target"].Captures[0].Value;
			string argument = match.Groups["argument"].Captures[0].Value;

			SetInputHiddenValue("__EVENTTARGET", target.Replace('$', ':'));
			SetInputHiddenValue("__EVENTARGUMENT", argument);
			Submit();
		}
	}

	public class ParseException : ApplicationException
	{
		internal ParseException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// The test is trying to perform a UI operation on a disabled control
	/// </summary>
	public class ControlDisabledException : InvalidOperationException
	{
		public ControlDisabledException(ControlTester control) :
			base(GetMessage(control))
		{
		}

		private static string GetMessage(ControlTester control)
		{
			return string.Format(
				"Control {0} (HTML ID: {1}; ASP location: {2}) is disabled",
				control.AspId, control.HtmlId, control.Description);
		}
	}
}
