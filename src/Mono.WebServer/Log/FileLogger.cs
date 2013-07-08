using System;
using System.IO;

namespace Mono.WebServer.Log {
	class FileLogger {
		StreamWriter writer;

		readonly object write_lock = new object ();

		~FileLogger ()
		{
			Close ();
		}

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

		public void Write (LogLevel level, string text)
		{
			if (writer == null)
				return;

			lock (write_lock) {
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
