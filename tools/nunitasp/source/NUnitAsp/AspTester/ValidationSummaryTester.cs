#region Copyright (c) 2002, 2003, Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2002, 2003 Brian Knowles, Jim Shore
' Originally written by David Paxson.  Copyright assigned to Brian Knowles and Jim Shore
' on the nunitasp-devl@lists.sourceforge.net mailing list on 28 Aug 2002.
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
	/// Tester for System.Web.UI.WebControls.ValidationSummary
	/// </summary>
	public class ValidationSummaryTester : AspControlTester
	{
		/// <summary>
		/// Create the tester and link it to an ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ID of the control to link to.</param>
		/// <param name="container">The control that contains the control to link to</param>
		public ValidationSummaryTester(string aspId, Tester container) 
		: base(aspId, container)
		{
		}

		/// <summary>
		/// The messages in the validation summary.
		/// </summary>
		public string[] Messages
		{
			get
			{
				if (Element.SelectSingleNode(".//ul") != null)
				{
					return ReadBulletedMessages();
				}
				else
				{
					return ReadListMessages();
				}
			}
		}

		private string[] ReadBulletedMessages()
		{
			XmlNodeList nodes = Element.SelectNodes(".//ul/li");
			string[] messages = new string[nodes.Count];
			for (int i = 0; i < nodes.Count; i++)
			{
				messages[i] = nodes[i].InnerXml;
			}
			return messages;
		}

		private string[] ReadListMessages() 
		{
			XmlNode node = Element.SelectSingleNode(".//font");
			string delim = "<br />";
			string inner = node.InnerXml.Trim();
			if (inner.Length >= delim.Length)
			{
				inner = inner.Substring(0, inner.Length - delim.Length);
			}
			return inner.Replace(delim, "|").Split('|');
		}
	}
}
