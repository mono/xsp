//
// Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

using System;
using System.Diagnostics;
using System.IO;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	public class Server
	{
		public static int Main (string [] args)
		{
			Type type = typeof (MonoApplicationHost);
			string cwd = Directory.GetCurrentDirectory ();
			MonoApplicationHost host;

			host =  (MonoApplicationHost) ApplicationHost.CreateApplicationHost (type, "/", cwd);
			host.SetListenAddress (8080);
			host.Start ();
			return 0;
		}
	}
}

