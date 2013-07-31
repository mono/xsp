//
// OnDemandServer.cs
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
using System;
using Mono.FastCgi;
using System.IO;
using Mono.WebServer.Log;
using System.Collections.Generic;

namespace Mono.WebServer.FastCgi
{
	public class FakeGenericServer<T> : IGenericServer<T> where T : IConnection
	{
		IGenericServer<T> backend;

		public event EventHandler RequestReceived;

		public FakeGenericServer (IGenericServer<T> server)
		{
			if (server == null)
				throw new ArgumentNullException ("server");
			backend = server;
		}

		public void Start (bool background, int backlog)
		{
		}

		public void Stop ()
		{
		}

		public void EndConnection(T connection)
		{
		}

		public int MaxConnections {
			get { return backend.MaxConnections; }
			set { backend.MaxConnections = value; }
		}

		public int ConnectionCount {
			get { return backend.ConnectionCount; }
		}

		public bool Started {
			get { return backend.Started; }
		}

		public bool CanAccept {
			get { return backend.CanAccept; }
		}

		public IEnumerable<T> Connections {
			get { return backend.Connections; }
		}
	}
}

