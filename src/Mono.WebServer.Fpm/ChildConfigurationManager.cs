//
// ChildConfigurationManager.cs:
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
using Mono.WebServer.Options.Settings;

namespace Mono.WebServer.Fpm {
	class ChildConfigurationManager : FastCgi.ConfigurationManager
	{
		readonly StringSetting user = new StringSetting ("user", String.Empty);
		readonly StringSetting group = new StringSetting ("group", String.Empty, defaultValue: "nobody");
		readonly StringSetting shimSocket = new StringSetting ("shimsock", String.Empty);

		readonly EnumSetting<InstanceType> instanceType = new EnumSetting<InstanceType>("instance-type", "The kind of instance (static, ondemand or dynamic)", defaultValue:InstanceType.Ondemand);

		public string User {
			get { return user; }
		}
		public string Group {
			get { return group; }
		}
		public string ShimSocket {
			get { return shimSocket; }
		}

		public InstanceType InstanceType {
			get { return instanceType; }
		}

		public ChildConfigurationManager (string name) : base (name)
		{
			Add (user, group, shimSocket, instanceType);
		}
	}
}
