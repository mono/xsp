//
// ConfigurationManager.cs: Generic multi-source configuration manager.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Robert Jordan <robertj@gmx.net>
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (C) 2007 Brian Nickel
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
using System.Reflection;
using System.Xml;
using Mono.WebServer.Options;

namespace Mono.WebServer {
	[Obsolete]
	public class ConfigurationManager
	{
		readonly FastCgi.ConfigurationManager configurationManager = new FastCgi.ConfigurationManager ("fastcgi-mono-server");

		public ConfigurationManager (Assembly asm, string resource)
		{
			if (asm == null)
				throw new ArgumentNullException ("asm");
			if (resource == null)
				throw new ArgumentNullException ("resource");

			var doc = new XmlDocument ();
			Stream stream = asm.GetManifestResourceStream (resource);
			if (stream != null)
				doc.Load (stream);
			configurationManager.ImportSettings (doc, false);
		}

		public bool Contains (string name)
		{
			return configurationManager.Contains (name);
		}

		[Obsolete]
		public object this [string name] {
			get {
				return configurationManager.GetSetting (name).Value;
			}
			
			set {
				configurationManager.SetValue (name, value);
			}
		}

		public void PrintHelp ()
		{
			configurationManager.PrintHelp ();
		}

		public void LoadCommandLineArgs (string[] args)
		{
			configurationManager.LoadCommandLineArgs (args);
		}

		public void LoadXmlConfig (string filename)
		{
			configurationManager.TryLoadXmlConfig (filename);
		}
	}
}
