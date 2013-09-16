//
// Logger.cs: Logs server events.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Leonardo Taglialegne <leonardo.taglialegne@gmail.com>
//
// Copyright (C) 2007 Brian Nickel
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Diagnostics;

namespace Mono.WebServer.Log {
	public static class Logger
	{
		static readonly List<ILogger> loggers = new List<ILogger> ();
		static readonly FileLogger file_logger = new FileLogger ();

		static readonly object write_lock = new object ();

		#region Public Static Properties

		public static LogLevel Level { get; set; }

		public static bool WriteToConsole { get; set; }

		public static bool Verbose { get; set; }

		public static string Name { get; set; }

		static int? ThreadId {
			get {
				if (Verbose && (Level & LogLevel.Debug) != LogLevel.None)
					return Thread.CurrentThread.ManagedThreadId;
				return null;
			}
		}


		static int? ProcessId {
			get {
				if (Verbose && (Level & LogLevel.Debug) != LogLevel.None)
					return Process.GetCurrentProcess ().Id;
				return null;
			}
		}
		#endregion
		
		static Logger ()
		{
			Level = LogLevel.Standard;
			loggers.Add (file_logger);
		}
		
		#region Public Static Methods

		public static void AddLogger (ILogger logger)
		{
			loggers.Add (logger);
		}
		
		public static void Open (string path)
		{
			file_logger.Open (path);
		}

		public static void Write (LogLevel level, string format, params object [] args)
		{
			Write (level, CultureInfo.CurrentCulture, format, args);
		}

		public static void Write (Exception e)
		{
			if (e == null)
				throw new ArgumentNullException ("e");
			Write (LogLevel.Error, e.Message);
			if(e.StackTrace != null)
				foreach(var line in e.StackTrace.Split(new []{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
					Write (LogLevel.Error, line);
		}

		static string GetFormatString ()
		{
			return ThreadId == null 
				? "[{2}] {3,-7}: {4}" 
				: Name == null
					? "{1,2} [{2}] {3,-7}: {4}"
					: "{0,5}:{1,2} {5,-8} [{2}] {3,-7}: {4}";
		}

		public static void Write (LogLevel level, string message)
		{
			if ((Level & level) == LogLevel.None)
				return;

			var format = GetFormatString ();
			string time = Verbose
				? DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss.ffffff")
				: DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
			string text = String.Format (CultureInfo.CurrentCulture, format, ProcessId, ThreadId, time, level, message, Name);

			if (WriteToConsole)
				lock (write_lock)
					if (Verbose)
						Console.WriteLine (text);
					else
						Console.WriteLine (message);

			foreach(var logger in loggers)
				logger.Write (level, text);
		}

		static void Write (LogLevel level,
		                   IFormatProvider provider,
		                   string format, params object [] args)
		{
			Write (level, String.Format (provider, format, args));
		}

		public static void Close ()
		{
			file_logger.Close ();
		}
		
		#endregion
	}
}
