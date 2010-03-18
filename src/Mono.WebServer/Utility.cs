//
// Mono.WebServer.Utility
//
// Authors:
//	Marek Habersack <mhabersack@novell.com>
//
// (C) Copyright 2010 Novell, Inc. (http://novell.com)
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
using System.Net.Sockets;

namespace Mono.WebServer
{
	public static class Utility
	{
		static readonly byte[] dummyRequestBuffer = new byte[] {0};
		
		public static bool SafeIsSocketConnected (Socket sock)
		{
			if (sock == null)
				return false;
			
			bool blocking = false;
			try {
				if (!sock.Connected)
					return false;

				blocking = sock.Blocking;
				sock.Blocking = false;
				sock.Receive (dummyRequestBuffer);

				return sock.Connected;
			} catch (SocketException ex) {
				if (ex.NativeErrorCode == 10035)
					return true;

				return false;
			} catch {
				// ignore
				return false;
			} finally {
				sock.Blocking = blocking;
			}
		}
	}
}
