//
// Web Server that uses ASP.NET hosting
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Web.Hosting;

namespace Mono.ASPNET
{
	public class Server
	{
		public static int Main (string [] args)
		{
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			Type type = typeof (MonoApplicationHost);
			string cwd = Directory.GetCurrentDirectory ();
			MonoApplicationHost host;

			host =  (MonoApplicationHost) ApplicationHost.CreateApplicationHost (type, "/", cwd);
			object o = ConfigurationSettings.AppSettings ["MonoServerPort"];
			if (o == null) {
				o = 8080;
			}

			ushort port;

			try {
				port = Convert.ToUInt16 (o);
			} catch (Exception e) {
				Console.WriteLine ("The value given for the listen port is not valid: " + o);
				return 1;
			}
			
			Console.WriteLine ("Listening on port: " + port);
			host.SetListenAddress (port);
			host.Start ();
			return 0;
		}
	}
}

