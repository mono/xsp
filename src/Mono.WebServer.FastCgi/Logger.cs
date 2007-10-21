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
	/// <summary>
	///   Specifies what type of message to log.
	/// </summary>
	[Flags]
	public enum LogLevel
	{
		/// <summary>
		///    No messages will be logged.
		/// </summary>
		None     = 0x00,
		
		/// <summary>
		///    Error messages will be logged.
		/// </summary>
		Error    = 0x01,
		
		/// <summary>
		///    Warning message will be logged.
		/// </summary>
		Warning  = 0x02,
		
		/// <summary>
		///    Notice messages will be logged.
		/// </summary>
		Notice   = 0x04,
		
		/// <summary>
		///    Debug messages will be logged.
		/// </summary>
		Debug    = 0x08,
		
		/// <summary>
		///    Standard messages will be logged.
		/// </summary>
		Standard = Error | Warning | Notice,
		
		/// <summary>
		///    All messages will be logged.
		/// </summary>
		All     = Error | Warning | Notice | Debug
	}
	
	
	
	/// <summary>
	///    This class stores log messages in a specified file.
	/// </summary>
	public class Logger
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the writer to ouput to.
		/// </summary>
		private StreamWriter writer;
		
		/// <summary>
		///    Indicates whether or not to write to the console.
		/// </summary>
		private bool write_to_console;
		
		/// <summary>
		///    Contains the bitwise combined log levels to write.
		/// </summary>
		private LogLevel level = LogLevel.Standard;
		
		/// <summary>
		///    Contains the lock object to use on the writer.
		/// </summary>
		private object write_lock = new object ();
		
		/// <summary>
		///    Contains the singleton instance of the writer.
		/// </summary>
		private static Logger logger = new Logger ();
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Finalizes the singleton instance by closing the stream.
		/// </summary>
		~Logger ()
		{
			Close ();
		}
		
		#endregion
		
		
		
		#region Public Static Properties
		
		/// <summary>
		///    Gets and sets the levels of messages to log.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="LogLevel" /> specifying the
		///    levels of events to log.
		/// </value>
		public static LogLevel Level {
			get {return logger.level;}
			set {logger.level = value;}
		}
		
		/// <summary>
		///    Gets and sets whether or not to write log messages to the
		///    console.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not log
		///    messages will be displayed in the console.
		/// </value>
		public static bool WriteToConsole {
			get {return logger.write_to_console;}
			set {logger.write_to_console = value;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Opens a file to log to.
		/// </summary>
		/// <param name="path">
		///    <para>A <see cref="String" /> containing the path of the
		///    file to open.</para>
		///    <para>This value is the same as the parameter that would
		///    be passed to <see cref="File.OpenWrite" />.</para>
		/// </param>
		/// <remarks>
		///    For information on what exceptions are thrown by this
		///    method, see <see cref="FileInfo.OpenWrite" />.
		/// </remarks>
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
		
		/// <summary>
		///    Writes a formatted string with a specified warning level
		///    to the log file, if one exists.
		/// </summary>
		/// <param name="level">
		///    A <see cref="LogLevel" /> containing the severity of the
		///    message.
		/// </param>
		/// <param name="provider">
		///    A <see cref="IFormatProvider" /> object to use when
		///    formatting values for the message.
		/// </param>
		/// <param name="format">
		///    A <see cref="string" /> containing the format to use for
		///    the message.
		/// </param>
		/// <param name="args">
		///    A <see cref="object[]" /> containing values to insert
		///    into the format.
		/// </param>
		/// <remarks>
		///    <para>The message will only be written to the log if
		///    <see cref="Level" /> contains <paramref name="level" />.
		///    </para>
		///    <para>See <see
		///    cref="string.Format(IFormatProvider,string,object[])" />
		///    for more details on this method's arguments.</para>
		/// </remarks>
		public static void Write (LogLevel level,
		                          IFormatProvider provider,
		                          string format, params object [] args)
		{
			Write (level, string.Format (provider, format, args));
		}
				
		/// <summary>
		///    Writes a formatted string with a specified warning level
		///    to the log file, if one exists.
		/// </summary>
		/// <param name="level">
		///    A <see cref="LogLevel" /> containing the severity of the
		///    message.
		/// </param>
		/// <param name="format">
		///    A <see cref="string" /> containing the format to use for
		///    the message.
		/// </param>
		/// <param name="args">
		///    A <see cref="object[]" /> containing values to insert
		///    into the format.
		/// </param>
		/// <remarks>
		///    <para>The message will only be written to the log if
		///    <see cref="Level" /> contains <paramref name="level" />.
		///    </para>
		///    <para>This method outputs using the current culture of
		///    the assembly. To use a different culture, use
		///    <see
		///    cref="Write(LogLevel,IFormatProvider,string,object[])"
		///    />.</para>
		///    <para>See <see cref="string.Format(string,object[])" />
		///    for more details on this method's arguments.</para>
		/// </remarks>
		public static void Write (LogLevel level, string format,
		                          params object [] args)
		{
			Write (level, CultureInfo.CurrentCulture, format, args);
		}
		
		/// <summary>
		///    Writes a formatted string with a specified warning level
		///    to the log file, if one exists.
		/// </summary>
		/// <param name="level">
		///    A <see cref="LogLevel" /> containing the severity of the
		///    message.
		/// </param>
		/// <param name="message">
		///    A <see cref="string" /> containing the message to write.
		/// </param>
		/// <remarks>
		///    <para>The message will only be written to the log if
		///    <see cref="Level" /> contains <paramref name="level" />.
		///    </para>
		/// </remarks>
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
		
		/// <summary>
		///    Closes the log file and flushes its output.
		/// </summary>
		/// <remarks>
		///    This method is called automatically when the class is
		///    destroyed.
		/// </remarks>
		public static void Close ()
		{
			lock (logger.write_lock) {
				if (logger.writer == null)
					return;
				
				logger.writer.Flush ();
				logger.writer.Close ();
				logger.writer = null;
			}
		}
		
		#endregion
	}
}
