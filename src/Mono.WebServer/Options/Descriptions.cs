//
// Descriptions.cs
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

namespace Mono.WebServer.Options {
	public static class Descriptions
	{
		public const string MaxConns = "Specifies the maximum number of concurrent connections the server should accept.";

		public const string LogLevels = "Specifies what log levels to log. It can be any of the following values, or multiple if comma separated:\n" +
			"* Debug\n" +
			"* Notice\n" +
			"* Warning\n" +
			"* Error\n" +
			"* Standard (Notice,Warning,Error)\n" +
			"* All (Debug,Standard)\n" +
			"This value is only used when \"logfile\" or \"printlog\" are set.";

		public const string Socket = "Specifies the type of socket to listen on. Valid values are \"pipe\", \"unix\", and \"tcp\".\n" +
			"\"pipe\" indicates to use piped socket opened by the web server when it spawns the application.\n" +
			"\"unix\" indicates that a standard unix socket should be opened.\n" +
			"    The file name can be specified in the \"filename\" argument or appended to this argument with a colon, eg:\n" +
			"    unix\n    unix:/tmp/fastcgi-mono-socket\n" +
			"\"tcp\" indicates that a TCP socket should be opened. " +
			"    The address and port can be specified in the \"port\" and \"address\" arguments or appended to this argument with a colon, eg:\n" +
			"    tcp\n    tcp:8081\n    tcp:127.0.0.1:8081\n    tcp:0.0.0.0:8081";

		public const string AppConfigFile = "Adds application definitions from an XML configuration file, typically with the \".webapp\" extension. " +
			"See sample configuration file that comes with the server.";

		public const string AppConfigDir = "Adds application definitions from all XML files found in the specified directory. " +
			"Files must have the \".webapp\" extension.";

		public const string ConfigFile = "Specifies a file containing configuration options, identical to those available in the command line.";

		public const string Stoppable = "Allows the user to stop the server by pressing \"Enter\". " +
			"This should not be used when the server has no controlling terminal.";

		internal const string Address = "Specifies the IP address to listen on.";

		public const string Port = "Specifies the TCP port number to listen on.";

		internal const string Root = "Specifies the root directory the server changes to before doing performing any operations.\n" +
			"This value is only used when \"appconfigfile\", \"appconfigdir\", or \"applications\" is set, to provide a relative base path.";

		// TODO: use markup (sigh) for better formatting
		public const string Applications = "Adds applications from a comma separated list of virtual and physical directory pairs. " +
			"The pairs are separated by colons and optionally include the virtual host name and port to use:\n" +
			"    [hostname:[port:]]VPath:realpath,...\n" +
			"Samples:\n" +
			"     /:.\n" +
			"    The virtual root directory, \"/\", is mapped to the current directory or \"root\" if specified." +
			"    /blog:../myblog\n" +
			"    The virtual /blog is mapped to ../myblog\n" +
			"    myhost.someprovider.net:/blog:../myblog\n" +
			"    The virtual /blog at myhost.someprovider.net is mapped to ../myblog.\n" +
			"    This means that other host names, like \"localhost\" will not be mapped.\n" +
			"    /:.,/blog:../myblog\n" +
			"    Two applications like the above ones are handled.\n" +
			"    *:80:/:standard,*:433:/:secure\n" +
			"    The server uses different applications on the unsecure and secure ports.";

		public const string Terminate = "Gracefully terminates a running mod-mono-server instance. " +
			"All other options but --filename or --address and --port are ignored if this option is provided.";

		public const string Master = "This instance will be used to by mod_mono to create ASP.NET applications on demand. " +
			"If this option is provided, there is no need to provide a list of applications to start.";
	}
}