//
// Mono.ASPNET.ModMonoTCPWebSource
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Mono.ASPNET
{
	public class ModMonoTCPWebSource: ModMonoWebSource
	{
		IPEndPoint bindAddress;

		public ModMonoTCPWebSource (IPAddress address, int port)
		{
			if (address == IPAddress.Any)
				address = IPAddress.Loopback;

			SetListenAddress (address, port);
		}
		
		public void SetListenAddress (int port)
		{
			SetListenAddress (IPAddress.Any, port);
		}

		public void SetListenAddress (IPAddress address, int port) 
		{
			SetListenAddress (new IPEndPoint (address, port));
		}

		public void SetListenAddress (IPEndPoint bindAddress)
		{
			if (bindAddress == null)
				throw new ArgumentNullException ("bindAddress");

			this.bindAddress = bindAddress;
		}

		public override bool GracefulShutdown ()
		{
			EndPoint ep = bindAddress;
			Socket sock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			try {
				sock.Connect (ep);
			} catch (Exception e) {
				Console.Error.WriteLine ("Cannot connect to {0}: {1}", ep, e.Message);
				return false;
			}

			return SendShutdownCommandAndClose (sock);
		}

		public override Socket CreateSocket ()
		{
			if (bindAddress == null)
				throw new InvalidOperationException ("No address/port to listen");

			Socket listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream,
							ProtocolType.IP);
			listen_socket.Bind (bindAddress);
			return listen_socket;
		}
	}
}

