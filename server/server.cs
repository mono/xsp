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
		public static void ShowHelp ()
		{
			Console.WriteLine ("XSP server is a sample server that hosts the ASP.NET runtime in a\n");
			Console.WriteLine ("minimalistic HTTP server\n\n");
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    xsp.exe [--port N]");
		}
		
		public static int Main (string [] args)
		{
			object oport;
			
			Trace.Listeners.Add (new TextWriterTraceListener (Console.Out));
			Type type = typeof (MonoApplicationHost);
			string cwd = Directory.GetCurrentDirectory ();
			MonoApplicationHost host;

			host =  (MonoApplicationHost) ApplicationHost.CreateApplicationHost (type, "/", cwd);
			oport = ConfigurationSettings.AppSettings ["MonoServerPort"];
			if (oport == null)
				oport = 8080;

			for (int i = 0; i < args.Length; i++){
				string a = args [i];
				
				switch (a){
				case "--port":
					oport = args [++i];
					break;
				case "--help":
					ShowHelp ();
					return 0;
				}
			}
			
			ushort port;

			try {
				port = Convert.ToUInt16 (oport);
			} catch (Exception e) {
				Console.WriteLine ("The value given for the listen port is not valid: " + oport);
				return 1;
			}
			
			Console.WriteLine ("Listening on port: " + port);
			host.SetListenAddress (port);
			host.Start ();
			return 0;
		}
	}
}

