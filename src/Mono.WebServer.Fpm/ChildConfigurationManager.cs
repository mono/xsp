using System;
using Mono.WebServer.Options;

namespace Mono.WebServer.Fpm {
	class ChildConfigurationManager : FastCgi.ConfigurationManager{
		public ChildConfigurationManager ()
		{
			Add (user);
		}

		readonly StringSetting user = new StringSetting ("user", String.Empty);

		public string User {
			get { return user; }
		}
	}
}