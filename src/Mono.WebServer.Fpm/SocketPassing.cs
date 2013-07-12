using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Mono.WebServer.Fpm {
	public static class SocketPassing
	{
		public static void SendTo (Socket connection, IntPtr passing)
		{
			send_fd (connection.Handle, passing);
		}

		public static Stream ReceiveFrom (Socket connection)
		{
			IntPtr fd;
			recv_fd (connection.Handle, out fd);
			return new UnixStream (fd.ToInt32 ());
		}

		[DllImportAttribute("fpm_helper")]
		static extern void send_fd(IntPtr sock, IntPtr fd);

		[DllImportAttribute("fpm_helper")]
		static extern void recv_fd(IntPtr sock, out IntPtr fd);
	}
}

