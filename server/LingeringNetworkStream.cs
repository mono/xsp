//
// Mono.ASPNET.ApplicationServer
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//


using System;
using System.Net.Sockets;

namespace Mono.ASPNET
{
	public class LingeringNetworkStream : NetworkStream 
	{
		const int useconds_to_linger = 2000000;
		const int max_useconds_to_linger = 30000000;
		bool enableLingering = true;

		public LingeringNetworkStream (Socket sock, bool owns) : base (sock, owns)
		{
		}
		
		public bool EnableLingering
		{
			get { return enableLingering; }
			set { enableLingering = value; }
		}

		void LingeringClose ()
		{
			int waited = 0;
			byte [] buffer = null;

			Socket.Shutdown (SocketShutdown.Send);
			while (waited < max_useconds_to_linger) {
				int nread = 0;
				try {
					if (!Socket.Poll (useconds_to_linger, SelectMode.SelectRead))
						break;

					if (buffer == null)
						buffer = new byte [512];

					nread = Socket.Receive (buffer, 0, buffer.Length, 0);
				} catch { }

				if (nread == 0)
					break;

				waited += useconds_to_linger;
			}
		}

		public override void Close ()
		{
			if (enableLingering) {
				try {
					LingeringClose ();
				} finally {
					base.Close ();
				}
			}
			else
				base.Close ();
		}

		public bool Connected {
			get { return Socket.Connected; }
		}
	}
}
