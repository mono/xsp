//
// WebTestHelper.cs
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

	public class WebTestHelper {

		private static string server;
		private bool teststatus;
		private StringBuilder sb;

		public string Server {
			get {
				if (server == null) {
					string host = Environment.GetEnvironmentVariable ("XSP_TEST_HOST");
					if ((host == null) || (host.Length == 0))
						host = "localhost";
					server = String.Concat ("http://", host, "/");
				}
				return server;
			}
		}

		public string Url (string path) 
		{
			return Server + path;
		}

		public HttpWebResponse Get (string url) 
		{
			HttpWebRequest wreq = (HttpWebRequest) WebRequest.Create (url);
			HttpWebResponse wresp = null;

			try {
				wresp = (HttpWebResponse) wreq.GetResponse ();
			}
			catch (WebException we) {
				wresp = (HttpWebResponse) we.Response;
			}

			if (wresp == null) {
				Assert.Ignore ("Couldn't reach url {0}{1}Check that the web server is up and running.", 
					new object[] { url, Environment.NewLine });
			}
			return wresp;
		}

		[SetUp]
		public void SetUp () 
		{
			teststatus = true;
			sb = new StringBuilder ();
		}

		[TearDown]
		public void TearDown () 
		{
			if (!teststatus) {
				Assert.Fail (sb.ToString ());
			}
		}

		public void Report (string url, string message) 
		{
			teststatus = false;
			sb.AppendFormat ("{1}{2}URL\t\t{0}{2}", url, message, Environment.NewLine);
		}

		public void Report (string url, HttpStatusCode status) 
		{
			teststatus = false;
			sb.AppendFormat ("WARNING: Unexpected HTTP Status Code{3}URL\t\t{0}{3}Status Code\t<{1}> {2}{3}",
				url, status, (int)status, Environment.NewLine);
		}

		private string SaveData (string data) 
		{
			string filename = Path.GetTempFileName ();
			using (StreamWriter sw = new StreamWriter (filename)) {
				sw.Write (data);
				sw.Close ();
			}
			return filename;
		}

		// verify that we we receive (if any) isn't the ASPX
		// source code (which should contain '<%' somewhere)
		public bool CheckForSourceCodeInResult (HttpWebResponse response)
		{
			if (response == null)
				return false;

			Stream s = response.GetResponseStream ();
			StreamReader sr = new StreamReader (s, Encoding.UTF8);
			string data = sr.ReadToEnd ();

			bool source = (data.IndexOf ("<%") >= 0);
			if (source) {
				teststatus = false;
				sb.Append ("WARNING: Probable Source Code Disclosure");
				string file = SaveData (data);
				sb.AppendFormat ("{0}Content saved in {1}", Environment.NewLine, file);
			}
			return source;
		}
	}
}
