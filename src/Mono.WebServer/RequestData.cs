//
// Mono.WebServer.RequestData
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004-2010 Novell, Inc
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

using System.Text;

namespace Mono.WebServer
{
	public class RequestData
	{
		public string Verb;
		public string Path;
		public string PathInfo;
		public string QueryString;
		public string Protocol;
		public byte [] InputBuffer;

		public RequestData (string verb, string path, string queryString, string protocol)
		{
			Verb = verb;
			Path = path;
			QueryString = queryString;
			Protocol = protocol;
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("Verb: {0}\n", Verb);
			sb.AppendFormat ("Path: {0}\n", Path);
			sb.AppendFormat ("PathInfo: {0}\n", PathInfo);
			sb.AppendFormat ("QueryString: {0}\n", QueryString);
			return sb.ToString ();
		}
	}
}
