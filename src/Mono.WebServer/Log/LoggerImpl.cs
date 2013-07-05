using System;
using System.Globalization;
using System.IO;

namespace Mono.WebServer.Log {
	class LoggerImpl {
		StreamWriter writer;

		readonly object write_lock = new object ();

		internal LoggerImpl ()
		{
			Level = LogLevel.Standard;
		}

		~LoggerImpl ()
		{
			Close ();
		}

		public LogLevel Level { get; set; }

		public bool WriteToConsole { get; set; }

		public void Open (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			lock (write_lock) {
				Close ();
				Stream stream = File.Open (path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				stream.Seek (0, SeekOrigin.End);
				writer = new StreamWriter (stream);
			}
		}

		public void Write (LogLevel level, string message)
		{
			if (writer == null && !WriteToConsole)
				return;

			if ((Level & level) == LogLevel.None)
				return;

			string text = String.Format (CultureInfo.CurrentCulture,
				"[{0:u}] {1,-7} {2}",
				DateTime.Now,
				level,
				message);

			lock (write_lock) {
				if (WriteToConsole)
					Console.WriteLine (text);

				if (writer == null)
					return;

				writer.WriteLine (text);
				writer.Flush ();
			}
		}

		public void Close ()
		{
			lock (write_lock) {
				if (writer == null)
					return;

				try {
					writer.Flush ();
					writer.Close ();
				} catch (ObjectDisposedException) {
					// Already done
				}
				writer = null;
			}
		}
	}
}
