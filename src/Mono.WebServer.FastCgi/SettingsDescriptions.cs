namespace Mono.WebServer.FastCgi {
	static class SettingsDescriptions {
		internal const string MaxConns =
			"Specifies the maximum number of concurrent connections the server should accept.";

		internal const string LogLevels =
			"Specifies what log levels to log. It can be any of the following values, or multiple if comma separated:\n" +
			"* Debug\n" +
			"* Notice\n" +
			"* Warning\n" +
			"* Error\n" +
			"* Standard (Notice,Warning,Error)\n" +
			"* All (Debug,Standard)\n" +
			"This value is only used when \"logfile\" or \"printlog\" are set.";

		internal const string Socket =
			"Specifies the type of socket to listen on. Valid values are \"pipe\", \"unix\", and \"tcp\".\n" +
			"\"pipe\" indicates to use piped socket opened by the web server when it spawns the application.\n" +
			"\"unix\" indicates that a standard unix socket should be opened.\n" +
			"    The file name can be specified in the \"filename\" argument or appended to this argument with a colon, eg:\n" +
			"    unix\n    unix:/tmp/fastcgi-mono-socket\n" +
			"\"tcp\" indicates that a TCP socket should be opened. " +
			"    The address and port can be specified in the \"port\" and \"address\" arguments or appended to this argument with a colon, eg:\n" +
			"    tcp\n    tcp:8081\n    tcp:127.0.0.1:8081\n    tcp:0.0.0.0:8081";

		internal const string ConfigFile =
			"Specifies a file containing configuration options, identical to those available in he command line.";

		internal const string Stoppable =
			"Allows the user to stop the server by pressing \"Enter\". " +
			"This should not be used when the server has no controlling terminal.";
	}
}