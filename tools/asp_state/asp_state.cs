//
// Mono.ASPNET.Tools.StateServer
//
// Author(s):
//  Jackson Harper (jackson@ximian.com)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
//

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;

namespace Mono.ASPNET.Tools {

	public class StateServer {
	
		private static string ServerName {
			get {
				return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly ().CodeBase);
			}				
		}	

		private static string configurationfile;		
		private static string ConfigurationFileName {
			get {
				if (configurationfile == null) {
					configurationfile = Assembly.GetEntryAssembly ().CodeBase + ".config";
					if (configurationfile.StartsWith("file://"))
						configurationfile = configurationfile.Substring(7);
				}
				return configurationfile;
			}
		}
		
		private static void ShowUsage()
		{
			Console.WriteLine(@"ERROR: {0} doesn't recognize any command line arguments!!!
				
Usage is:
    {0}
	
It loads the remoting configuration file (will try from {1})
and works until <Enter> is pressed.
				", ServerName, ConfigurationFileName);
		}

		private static void ShowVerboseConfigurationInfo(string filename)
		{
			Console.WriteLine("Loaded configuration from {0} that contains", filename);
			Console.WriteLine("=============================================");
			try {
				StreamReader sr = new StreamReader(filename);
				Console.WriteLine(sr.ReadToEnd());
				sr.Close();
			} catch (Exception ex) {
				Console.WriteLine("ERROR reading configuration file:\n" + ex.ToString());
			}
			Console.WriteLine("=============================================");
		}

 		[STAThread]
		public static void Main (string [] args)
		{
			if (args.Length == 0) {
				RemotingConfiguration.Configure (ConfigurationFileName, false);
				ShowVerboseConfigurationInfo(ConfigurationFileName);
				Console.Write("Press <Enter> to stop...");
				Console.ReadLine ();
			} else {
				ShowUsage();
			}
		}

	}

}

