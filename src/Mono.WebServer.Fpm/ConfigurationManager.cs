//
// ConfigurationManager.cs:
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

using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	class ConfigurationManager : Options.ConfigurationManager, IHelpConfigurationManager
	{
		#region Backing fields
		readonly BoolSetting stoppable = new BoolSetting ("stoppable", Descriptions.Stoppable);

		readonly StringSetting configDir = new StringSetting ("config-dir", "Directory containing the configuration files.");
		readonly StringSetting fastCgiCommand = new StringSetting ("fastcgi-command", "Name (if in PATH) or full path of the fastcgi executable", defaultValue: "fastcgi-mono-server");
		#endregion

		#region Typesafe properties
		public bool Stoppable {
			get { return stoppable; }
		}

		public string ConfigDir {
			get { return configDir; }
		}
		public string FastCgiCommand {
			get { return fastCgiCommand; }
		}
		#endregion

		public string Name {
			get { return "mono-fpm"; }
		}

		public string Description {
			get { return "mono-fpm is a FastCgi process manager."; }
		}

		public ConfigurationManager ()
		{
			Add (stoppable,
				configDir, fastCgiCommand);
		}
	}
}
