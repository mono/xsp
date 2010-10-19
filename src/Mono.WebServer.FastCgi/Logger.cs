//
// Logger.cs: Logs server events.
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
using System.IO;
using System.Text;
using System.Globalization;

namespace Mono.FastCgi {
	[Flags]
	public enum LogLevel
	{
		None     = 0x00,
		
		Error    = 0x01,
		
		Warning  = 0x02,
		
		Notice   = 0x04,
		
		Debug    = 0x08,
		
		Standard = Error | Warning | Notice,
		
		All     = Error | Warning | Notice | Debug
	}
	
	
	
	public class Logger
	{
		#region Private Fields
		
		private StreamWriter writer;
		
		private bool write_to_console;
		
		private LogLevel level = LogLevel.Standard;
		
		private object write_lock = new object ();
		
		private static Logger logger = new Logger ();
		
		#endregion
		
		
		
		#region Private Methods
		
		~Logger ()
		{
			Close ();
		}
		
		#endregion
		
		
		
		#region Public Static Properties
		
		public static LogLevel Level {
			get {return logger.level;}
			set {logger.level = value;}
		}
		
		public static bool WriteToConsole {
			get {return logger.write_to_console;}
			set {logger.write_to_console = value;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		public static void Open (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			
			lock (logger.write_lock) {
				Close ();
				Stream stream = File.Open (path,
					FileMode.Append, FileAccess.Write,
					FileShare.ReadWrite);
				stream.Seek (0, SeekOrigin.End);
				logger.writer = new StreamWriter (stream);
			}
		}
		
		public static void Write (LogLevel level,
		                          IFormatProvider provider,
		                          string format, params object [] args)
		{
			Write (level, string.Format (provider, format, args));
		}
				
		public static void Write (LogLevel level, string format,
		                          params object [] args)
		{
			Write (level, CultureInfo.CurrentCulture, format, args);
		}
		
		public static void Write (LogLevel level, string message)
		{
			if (logger.writer == null && !logger.write_to_console)
				return;
			
			if ((Level & level) == LogLevel.None)
				return;
			
			string text = string.Format (CultureInfo.CurrentCulture,
				Strings.Logger_Format,
				DateTime.Now,
				level,
				message);
			
			lock (logger.write_lock) {
				if (logger.write_to_console)
					Console.WriteLine (text);
				
				if (logger.writer != null) {
					logger.writer.WriteLine (text);
					logger.writer.Flush ();
				}
			}
		}
		
		public static void Close ()
		{
			lock (logger.write_lock) {
				if (logger.writer == null)
					return;
				
				try {
					logger.writer.Flush ();
					logger.writer.Close ();
				} catch (System.ObjectDisposedException) {
					// Already done
				}
				logger.writer = null;
			}
		}
		
		#endregion
	}
}
