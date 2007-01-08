//
// FileExtensionsTest.cs
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Net;
using System.Text;

using NUnit.Framework;

namespace Tests.XSP.Security {

	// We try to manipulate the file extension so the web server doesn't
	// process (compile) the ASPX source code but load the file and 
	// return it (uncompiled source code) to the attacker.
	[TestFixture]
	public class FileExtensionsTest : WebTestHelper {

		// NTFS support multiple data stream
		// using ::$DATA could reveal the web page source code
		// http://support.microsoft.com/default.aspx?scid=kb;EN-US;188806
		[Test]
		public void AlternateDataStream_188806 () 
		{
			string url = Url ("index.aspx::$DATA");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
			case HttpStatusCode.InternalServerError:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// NTFS support multiple data stream
		// using :$DATA could reveal the web page source code
		// http://support.microsoft.com/default.aspx?scid=kb;EN-US;193793
		[Test]
		public void AlternateDataStream_193793 () 
		{
			string url = Url ("index.aspx:$DATA");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
			case HttpStatusCode.InternalServerError:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// Here we're trying to mess with the file extension decoding
		// algorithm by adding "ignored" characters (by some file 
		// systems) at the end of the URL and possibly receive the 
		// uncompiled source code.
		[Test]
		public void TrailingDot () 
		{
			string url = Url ("index.aspx.");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.OK:
			case HttpStatusCode.NotFound:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// Here we're trying to mess with the file extension decoding
		// algorithm by adding "ignored" characters (by some file 
		// systems) at the end of the URL and possibly receive the 
		// uncompiled source code.
		[Test]
		public void TrailingSlash () 
		{
			string url = Url ("index.aspx/");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.OK:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// Here we're trying to mess with the file extension decoding
		// algorithm by adding "ignored" characters (by some file 
		// systems) at the end of the URL and possibly receive the 
		// uncompiled source code.
		[Test]
		public void TrailingBackslash () 
		{
			string url = Url ("index.aspx\\");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.OK:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// Here we're trying to mess with the file extension decoding
		// algorithm by adding "ignored" characters (by some file 
		// systems) at the end of the URL and possibly receive the 
		// uncompiled source code.
		[Test]
		public void TrailingEncodedSpace () 
		{
			string url = Url ("index.aspx%20");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// Here we're trying to mess with the file extension decoding
		// algorithm by encoding the '.aspx' extension.
		// </summary>
		public void EncodedExtension () 
		{
			string url = Url ("index.%61%73%70%78");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.OK:
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}

		// Here we're trying to mess with the file extension decoding
		// algorithm by playing with the case.
		[Test]
		public void CaseSensitiveness () 
		{
			string url = Url ("index.AsPx");
			HttpWebResponse response = Get (url);
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
				// some file system are case-sensitive
				break;
			case HttpStatusCode.OK:
				// some file system are not case-sensitive
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			CheckForSourceCodeInResult (response);
		}
	}
}
