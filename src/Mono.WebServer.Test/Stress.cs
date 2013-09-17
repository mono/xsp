//
// Stress.cs
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
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace Mono.WebServer.Test {
	[TestFixture]
	public class Stress
	{
		[SetUp]
		public void Init ()
		{
			Utilities.LoadAssemblies ();
			Utilities.CopyLoadedAssemblies ();
			Utilities.SetLogToFail ();
		}

		[Test]
		public void ValidConnections ()
		{
			using (var server = new DebugServer()) {
				Assert.AreEqual (0, server.Run ());
				const int count = 1000;
				var threads = new Thread [count];
				try {
					for (int i = 0; i < count; i++) {
						threads [i] = new Thread (() => {
							var wc = new WebClient ();
							string downloaded = wc.DownloadString ("http://localhost:9000/");
							Assert.AreEqual (Environment.CurrentDirectory, downloaded);
						});
						threads [i].Start ();
					}

					foreach (Thread thread in threads)
						thread.Join ();
				} catch (WebException e) {
					Assert.Fail (e.Message);
				}
			}
		}

		[Test]
		public void InvalidConnections ()
		{
			using (var server = new DebugServer()) {
				Assert.AreEqual (0, server.Run ());
				const int count = 1000;
				var threads = new Thread [count];
				try {
					for (int i = 0; i < count; i++) {
						threads [i] = new Thread (() => {
							var client = new TcpClient ("localhost", 9000);
							using (var sw = new StreamWriter(client.GetStream()))
								sw.Write ("\0\0\0\0");
							client.Close ();
						});
						threads [i].Start ();
					}

					foreach (Thread thread in threads)
						thread.Join ();
				} catch (WebException e) {
					Assert.Fail (e.Message);
				}
			}
		}
	}
}

