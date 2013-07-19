//
// Watchdog.cs: A generic watchdog
//
// Authors:
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// (C) Copyright 2013 Leonardo Taglialegne
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

using System.Timers;

namespace Mono.WebServer.FastCgi
{
	class Watchdog
	{
		/// <summary>
		/// Gets the timeout.
		/// </summary>
		/// <value>The timeout, in milliseconds.</value>
		public double Timeout {
			get;
			private set;
		}

		public event ElapsedEventHandler End;

		Timer timer;

		readonly object end_lock = new object ();

		bool ended;

		/// <param name="timeout">Timeout, in milliseconds.</param>
		public Watchdog(double timeout)
		{
			Timeout = timeout;
			timer = CreateTimer (timeout);
		}

		public void Kick()
		{
			lock (end_lock) {
				if (ended)
					return;

				timer.Dispose ();
				timer = CreateTimer (Timeout);
			}
		}

		Timer CreateTimer (double timeout)
		{
			var toret = new Timer (timeout) {
				AutoReset = false
			};
			toret.Elapsed += (sender, args) =>  {
				lock (end_lock) {
					ended = true;
				}
				if (End != null)
					End (sender, args);
			};
			toret.Start ();
			return toret;
		}
	}
}
