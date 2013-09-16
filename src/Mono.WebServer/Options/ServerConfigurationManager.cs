//
// ServerConfigurationManager.cs
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
using System.Net;
using Mono.WebServer.Options.Settings;

namespace Mono.WebServer.Options {
	public abstract class ServerConfigurationManager : ConfigurationManager, IHelpConfigurationManager
	{
		#region Backing fields
		protected readonly StringSetting root = new StringSetting ("root", Descriptions.Root, "MonoServerRootDir", "MONO_ROOT|MONO_FCGI_ROOT", Environment.CurrentDirectory);
		readonly StringSetting applications = new StringSetting ("applications", Descriptions.Applications, "MonoApplications", "MONO_APPLICATIONS|MONO_FCGI_APPLICATIONS");
		readonly StringSetting appConfigFile = new StringSetting ("appconfigfile", Descriptions.AppConfigFile, "MonoApplicationsConfigFile", "MONO_APPCONFIGFILE|MONO_FCGI_APPCONFIGFILE");
		readonly StringSetting appConfigDir = new StringSetting ("appconfigdir", Descriptions.AppConfigDir, "MonoApplicationsConfigDir", "MONO_APPCONFIGDIR|MONO_FCGI_APPCONFIGDIR");

		readonly UInt32Setting backlog = new UInt32Setting ("backlog", "The listen backlog.", defaultValue: 500);

		protected readonly Setting<IPAddress> address = new Setting<IPAddress> ("address", IPAddress.TryParse, Descriptions.Address, "MonoServerAddress", "MONO_ADDRESS|MONO_FCGI_ADDRESS", IPAddress.Loopback);
		#endregion

		#region Typesafe properties
		public string Root {
			get { return root; }
		}
		public string Applications {
			get { return applications; }
		}
		public string AppConfigFile {
			get { return appConfigFile; }
		}
		public string AppConfigDir {
			get { return appConfigDir; }
		}

		public uint Backlog {
			get { return backlog; }
		}

		public IPAddress Address { get { return address; } }
		#endregion

		public abstract string ProgramName { get; }
		public abstract string Description { get; }

		protected ServerConfigurationManager (string name) : base (name)
		{
			Add (root, applications, appConfigFile, appConfigDir,
				backlog,
				address);
		}
	}
}
