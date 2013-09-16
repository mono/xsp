//
// ConfigurationManager.cs: Generic multi-source configuration manager.
//
// Author:
//   Leonardo Taglialegne <leonardo.taglialene@gmail.com>
//
// Copyright (C) 2013 Leonardo Taglialegne
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
using Mono.WebServer.Options;

namespace Mono.WebServer.FastCgi {
	public partial class ConfigurationManager
	{
		public ConfigurationManager (string name) : base (name)
		{
			Add(stoppable, multiplex, onDemand,
				maxConns, maxReqs, port, idleTime,
				filename, socket, onDemandSock);
		}

		#region Backing fields
		readonly BoolSetting stoppable = new BoolSetting ("stoppable", Descriptions.Stoppable);
		readonly BoolSetting multiplex = new BoolSetting ("multiplex", "Allows multiple requests to be send over a single connection.",
			"FastCgiMultiplexConnections", "MONO_FCGI_MULTIPLEX");
		readonly BoolSetting onDemand = new BoolSetting ("ondemand", "Listen on the socket specified via /ondemandsock and accepts via sendmsg(2). Terminates after it receives no requests for some time");

		readonly UInt16Setting maxConns = new UInt16Setting ("maxconns", Descriptions.MaxConns,
			"FastCgiMaxConnections", "MONO_FCGI_MAXCONNS", 1024);
		readonly UInt16Setting maxReqs = new UInt16Setting ("maxreqs", "Specifies the maximum number of concurrent requests the server should accept.",
			"FastCgiMaxRequests", "MONO_FCGI_MAXREQS", 1024);
		readonly UInt16Setting port = new UInt16Setting ("port", Descriptions.Port, "MonoServerPort", "MONO_FCGI_PORT", 9000);
		readonly UInt16Setting idleTime = new UInt16Setting ("idle-time", "Time to wait (in seconds) before stopping if --ondemand is set", defaultValue: 60);

		readonly StringSetting filename = new StringSetting ("filename", "Specifies a unix socket filename to listen on.\n" +
			"To use this argument, \"socket\" must be set to \"unix\".", "MonoUnixSocket", "MONO_FCGI_FILENAME", "/tmp/fastcgi-mono-server");
		readonly StringSetting socket = new StringSetting ("socket", Descriptions.Socket, "MonoSocketType", "MONO_FCGI_SOCKET", "pipe");
		readonly StringSetting onDemandSock = new StringSetting ("ondemandsock", "The socket to listen on for ondemand service");
		#endregion

		#region Typesafe properties
		public bool Stoppable {
			get { return stoppable; }
		}
		public bool Multiplex {
			get { return multiplex; }
		}
		public bool OnDemand {
			get { return onDemand; }
		}

		public ushort MaxConns {
			get { return maxConns; }
		}
		public ushort MaxReqs {
			get { return maxReqs; }
		}
		public ushort Port {
			get { return port; }
		}
		public ushort IdleTime {
			get { return idleTime; }
		}

		public string Filename {
			get { return filename; }
		}
		public string Socket {
			get { return socket; }
		}
		public string OnDemandSock {
			get { return onDemandSock; }
		}

		/*
		 * <Setting Name="automappaths" AppSetting="MonoAutomapPaths"
		 * Environment="MONO_FCGI_AUTOMAPPATHS" Type="Bool" ConsoleVisible="True" Value="False">
		 * <Description>
		 * <para>Automatically registers applications as they are
		 * encountered, provided pages exist in standard
		 * locations.</para>
		 * </Description>
		 * </Setting>
		 */
		#endregion

		public override string ProgramName {
			get { return "mono-fastcgi"; }
		}

		public override string Description {
			get { return "A FastCgi interface for ASP.NET applications."; }
		}
	}
}
