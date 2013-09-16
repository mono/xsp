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

using Mono.WebServer.Options.Settings;
using Mono.Unix;
using System;
using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	class ConfigurationManager : Options.ConfigurationManager, IHelpConfigurationManager
	{
		#region Backing fields
		readonly BoolSetting stoppable = new BoolSetting ("stoppable", Descriptions.Stoppable);

		readonly UInt16Setting childIdleTime = new UInt16Setting ("child-idle-time", "Time to wait (in seconds) before stopping a child started with --web-dir", defaultValue: 60);

		readonly StringSetting configDir = new StringSetting ("config-dir|configdir", "Directory containing the configuration files.");
		readonly StringSetting fastCgiCommand = new StringSetting ("fastcgi-command", "Name (if in PATH) or full path of the fastcgi executable", defaultValue: "fastcgi-mono-server4");
		readonly StringSetting shimCommand = new StringSetting ("shim-command", "Name (if in PATH) or full path of the shim executable", defaultValue: "shim");
		readonly StringSetting fpmUser = new StringSetting ("fpm-user", "Name of the user to use for the fpm daemon", defaultValue: "fpm");
		readonly StringSetting fpmGroup = new StringSetting ("fpm-group", "Name of the group to use for the fpm daemon", defaultValue: "fpm");
		readonly StringSetting webDir = new StringSetting ("web-dir|webdir", "Directory containing the user web directories.");
		readonly StringSetting webGroup = new StringSetting ("web-group", "Name of the group to use for the web directories daemons", defaultValue: "nobody");
		readonly StringSetting httpdGroup = new StringSetting ("httpd-group", "Name of the httpd group to use for the web sockets dir", defaultValue: HttpdEuristic ());
		#endregion

		#region Typesafe properties
		public bool Stoppable {
			get { return stoppable; }
		}

		public ushort ChildIdleTime {
			get { return childIdleTime; }
		}

		public string ConfigDir {
			get { return configDir; }
		}
		public string FastCgiCommand {
			get { return fastCgiCommand; }
		}
		public string ShimCommand {
			get { return shimCommand; }
		}
		public string FpmUser {
			get { return fpmUser; }
		}
		public string FpmGroup {
			get { return fpmGroup; }
		}
		public string WebDir {
			get { return webDir; }
		}
		public string WebGroup {
			get { return webGroup; }
		}
		public string HttpdGroup {
			get { return httpdGroup; }
		}
		#endregion

		public string ProgramName {
			get { return "mono-fpm"; }
		}

		public string Description {
			get { return "mono-fpm is a FastCgi process manager."; }
		}

		public ConfigurationManager (string name) : base(name)
		{
			Add (stoppable, childIdleTime, configDir, fastCgiCommand, fpmUser, fpmGroup, webDir, webGroup, httpdGroup);
		}

		static readonly string[] knownHttpdGroups = {"www-data", "nginx", "apache2", "http", "www"};

		static string HttpdEuristic ()
		{
			if (Platform.IsUnix)
				foreach (string group in knownHttpdGroups) {
					try {
						new UnixGroupInfo (group);
						return group;
					} catch (ArgumentException) {
					}
				}
			return null;
		}
	}
}
