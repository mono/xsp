//
// ConfigurationManagerExtensions.cs
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (c) 2013 Leonardo Taglialegne.
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
using System.Collections.Generic;
using NDesk.Options;

namespace Mono.WebServer.Options {
	public static class ConfigurationManagerExtensions {

		public static void PrintHelp<T> (this T configurationManager) where T : IHelpConfigurationManager
		{
			Version.Show ();
			Console.WriteLine (configurationManager.Description);
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    {0} [...]", configurationManager.ProgramName);
			Console.WriteLine ();
			configurationManager.CreateOptionSet ().WriteOptionDescriptions (Console.Out);
		}

		public static bool LoadCommandLineArgs<T> (this T configurationManager, string [] cmd_args) where T : IHelpConfigurationManager
		{
			if (cmd_args == null)
				throw new ArgumentNullException ("cmd_args");

			OptionSet optionSet = configurationManager.CreateOptionSet ();

			List<string> extra;
			try {
				extra = optionSet.Parse (cmd_args);
			} catch (OptionException e) {
				Console.WriteLine ("{0}: {1}", configurationManager.ProgramName, e.Message);
				Console.WriteLine ("Try `{0} --help' for more information.", configurationManager.ProgramName);
				return false;
			}

			if (extra.Count > 0) {
				Console.Write ("Warning: unparsed command line arguments: ");
				foreach (string s in extra) {
					Console.Write ("{0} ", s);
				}
				Console.WriteLine ();
			}

			return true;
		}
	}
}
