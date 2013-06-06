//
// SocketAbstractions/UnmanagedSocket.cs: Provides a wrapper around an unmanaged
// socket.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

using System;
using sock=System.Net.Sockets;
using Mono.Unix.Native;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mono.FastCgi {
	internal class UnmanagedSocket : Socket
	{
		[DllImport ("libc", SetLastError=true, EntryPoint="close")] 
		extern static int close (IntPtr s);

		readonly IntPtr socket;
		bool connected;
		
		unsafe public UnmanagedSocket (IntPtr socket)
		{
			if (!supports_libc)
				throw new NotSupportedException (
					Strings.UnmanagedSocket_NotSupported);
			
			if ((int) socket < 0)
				throw new ArgumentException ("Invalid socket.",
					"socket");
			
			var address = new byte [1024];
			int size = 1024;
			fixed (byte* ptr = address)
				if (getsockname (socket, ptr, ref size) != 0)
					throw GetException ();
		
		
			this.socket = socket;
		}
		
		public override void Close ()
		{
			connected = false;
			if (shutdown (socket, (int) sock.SocketShutdown.Both) != 0)
				throw GetException ();
			/* lighttpd makes the assumption that the FastCGI handler will close the unix socket */
			close(socket);
		}
		
		unsafe public override int Receive (byte [] buffer, int offset,
		                                    int size,
		                                    sock.SocketFlags flags)
		{
			if (!connected)
				return 0;
			
			int value;
			fixed (byte* ptr = buffer)
				value = recv (socket, ptr + offset, size, (int) flags);
			
			if (value >= 0)
				return value;
			
			connected = false;
			throw GetException ();
		}
		
		unsafe public override int Send (byte [] data, int offset,
		                                 int size,
		                                 sock.SocketFlags flags)
		{
			if (!connected)
				return 0;
			
			int value;
			fixed (byte* ptr = data)
				value = send (socket, ptr + offset, size,
					(int) flags);
			
			if (value >= 0)
				return value;
			
			connected = false;
			throw GetException ();
		}
		
		public override void Listen (int backlog)
		{
			listen (socket, backlog);
		}
		
		public override IAsyncResult BeginAccept (AsyncCallback callback,
		                                 object state)
		{
			var s = new SockAccept (socket, callback, state);
			ThreadPool.QueueUserWorkItem (s.Run);
			return s;
		}
		
		public override Socket EndAccept (IAsyncResult asyncResult)
		{
			if (asyncResult == null)
				throw new ArgumentNullException ("asyncResult");
			
			var s = asyncResult as SockAccept;
			if (s == null || s.socket != socket)
				throw new ArgumentException (
					"Result was not produced by current instance.",
					"asyncResult");
			
			if (!s.IsCompleted)
				s.AsyncWaitHandle.WaitOne ();
			
			if (s.except != null)
				throw s.except;
			
			return new UnmanagedSocket (s.accepted) {connected = true};
		}
		
		public override bool Connected {
			get {return connected;}
		}
		
		[DllImport ("libc", SetLastError=true, EntryPoint="shutdown")]
		extern static int shutdown (IntPtr s, int how);
		
		[DllImport ("libc", SetLastError=true, EntryPoint="send")]
		unsafe extern static int send (IntPtr s, byte *buffer, int len,
		                               int flags);
		
		[DllImport ("libc", SetLastError=true, EntryPoint="recv")]
		unsafe extern static int recv (IntPtr s, byte *buffer, int len,
		                               int flags);
		
		[DllImport ("libc", SetLastError=true, EntryPoint="accept")]
		unsafe extern static IntPtr accept (IntPtr s, byte *addr,
		                                    ref int addrlen);
		
		[DllImport("libc", SetLastError=true, EntryPoint="getsockname")]
		unsafe static extern int getsockname(IntPtr s, byte *addr,
		                                     ref int namelen);
		
		[DllImport ("libc", SetLastError=true, EntryPoint="listen")]
		extern static int listen (IntPtr s, int count);
		
		class SockAccept : IAsyncResult
		{
			bool completed;
			ManualResetEvent waithandle;
			readonly AsyncCallback callback;
			readonly object state;
			public readonly IntPtr socket;
			public IntPtr accepted;
			public sock.SocketException except;
			
			public SockAccept (IntPtr socket, AsyncCallback callback,
			                   object state)
			{
				this.socket = socket;
				this.callback = callback;
				this.state = state;
			}
			
			unsafe public void Run (object state)
			{
				var address = new byte [1024];
				int size = 1024;
				Errno errno;
				fixed (byte* ptr = address)
					do {
						accepted = accept (socket, ptr,
							ref size);
						errno = Stdlib.GetLastError ();
					} while ((int) accepted == -1 &&
						errno == Errno.EINTR);
				
				if ((int) accepted == -1)
					except = GetException (errno);
				
				completed = true;
				
				if (waithandle != null)
					waithandle.Set ();
				
				if (callback != null)
					callback (this);
			}
			
			public bool IsCompleted {
				get {return completed;}
			}
			
			public bool CompletedSynchronously {
				get {return false;}
			}
			
			public WaitHandle AsyncWaitHandle {
				get {
					lock (this)
						if (waithandle == null)
							waithandle = new ManualResetEvent (completed);
					
					return waithandle;
				}
			}
			
			public object AsyncState {
				get {return state;}
			}
		}
		
		static readonly bool supports_libc;
		
		static UnmanagedSocket ()
		{
			try {
				string os = "";
				using (Stream st = File.OpenRead (
					"/proc/sys/kernel/ostype")) {
					var sr = new StreamReader (st);
					os = sr.ReadToEnd ();
				}
				supports_libc = os.StartsWith ("Linux");
			} catch {
			}
		}
		
		static sock.SocketException GetException ()
		{
			return GetException (Stdlib.GetLastError ());
		}
		
		static sock.SocketException GetException (Errno error)
		{
			if (error == Errno.EAGAIN ||
				error == Errno.EWOULDBLOCK) // WSAEWOULDBLOCK
				return new sock.SocketException (10035);
			
			if (error == Errno.EBADF ||
				error == Errno.ENOTSOCK) // WSAENOTSOCK
				return new sock.SocketException (10038);
			
			if (error == Errno.ECONNABORTED) // WSAENETDOWN
				return new sock.SocketException (10050);
			
			if (error == Errno.EINVAL) // WSAEINVAL
				return new sock.SocketException (10022);
			
			return new sock.SocketException ();
		}
	}
}
