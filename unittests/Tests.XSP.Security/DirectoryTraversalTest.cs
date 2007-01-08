//
// DirectoryTraversalTest.cs
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

	// Here we try to manipulate the URL path so we can go outside the 
	// web server root and access some interesting files (e.g. passwords).
	// The file NOACCESS must be installed for the test results to be
	// meaningful.
	[TestFixture]
	public class DirectoryTraversalTest : WebTestHelper {

		// helper for directory traversals
		private HttpWebResponse AccessOutsideRoot (string url) 
		{
			string completeUrl = Url (url);
			HttpWebResponse response = Get (completeUrl);
			switch (response.StatusCode) {
			case HttpStatusCode.InternalServerError:
			case HttpStatusCode.NotFound:
				break;
			case HttpStatusCode.OK:
				Report (url, "ERROR: Access to outside the web root directory was possible!");
				break;
			default:
				// report any other error for analysis
				Report (url, response.StatusCode);
				break;
			}
			return response;
		}

		// Here we're trying to access a file outside the web root 
		// path.
		[Test]
		public void DotDot () 
		{
			AccessOutsideRoot (Url ("../NOACCESS"));
		}

		// Here we're trying to access a file outside the web root 
		// path using encoded '..'.
		[Test]
		public void DotDotEncoded () 
		{
			AccessOutsideRoot (Url ("%2e%2e/NOACCESS"));
		}

		// Here we're trying to access a file outside the web root 
		// path using encoded '../'.
		[Test]
		public void DotDotSlashEncoded () 
		{
			AccessOutsideRoot (Url ("%2e%2e%2fNOACCESS"));
		}

		// Here we're trying to access a file outside the web root 
		// path using '..\' (backslash).
		[Test]
		public void Backslash () 
		{
			AccessOutsideRoot (Url ("..\\NOACCESS"));
		}

		// Here we're trying to access a file outside the web root 
		// path using encoded '\'.
		[Test]
		public void EncodedBackslash () 
		{
			AccessOutsideRoot (Url ("..%5CNOACCESS"));
		}

		// Here we're trying to access a file outside the web root 
		// path using an overlong UTF8 sequence (resolving to '/').
		[Test]
		public void EncodedOverlongUTF8Slash () 
		{
			AccessOutsideRoot (Url ("..%c0%af5CNOACCESS"));
		}

		// Here we're trying to access a file from the root of the
		// server (but using a lot of slashes).
		// ref: http://bugzilla.ximian.com/show_bug.cgi?id=78119
		[Test]
		public void MultipleSlash ()
		{
			AccessOutsideRoot (Url ("appl/(2001992881)////etc/passwd"));
		}
	}
}
