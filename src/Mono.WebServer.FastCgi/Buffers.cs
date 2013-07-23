using System;

namespace Mono.WebServer.FastCgi
{
	public struct Buffers
	{
		public CompatArraySegment<byte>? Header { get; private set; }
		public CompatArraySegment<byte>? Body { get; private set; }
		public CompatArraySegment<byte>? Padding { get; private set; }

		readonly BufferManager bigBufferManager;
		readonly BufferManager smallBufferManager;

		public Buffers (BufferManager bigBufferManager, BufferManager smallBufferManager)
			: this ()
		{
			if (bigBufferManager == null)
				throw new ArgumentNullException ("bigBufferManager");
			if (smallBufferManager == null)
				throw new ArgumentNullException ("smallBufferManager");

			this.bigBufferManager = bigBufferManager;
			this.smallBufferManager = smallBufferManager;
			Header = smallBufferManager.ClaimBuffer ();
			Body = bigBufferManager.ClaimBuffer ();
			Padding = smallBufferManager.ClaimBuffer ();
		}

		public Buffers (byte[] buffer, int headerSize, int bodySize) : this ()
		{
			Header = MaybeSegment (buffer, 0, headerSize);
			Body = MaybeSegment (buffer, headerSize, bodySize);
			Padding = MaybeSegment (buffer, headerSize + bodySize);
		}

		static CompatArraySegment<byte>? MaybeSegment (byte[] buffer, int offset)
		{
			if (buffer == null)
				return null;
			return MaybeSegment (buffer, offset, buffer.Length - offset);
		}

		static CompatArraySegment<byte>? MaybeSegment (byte[] buffer, int offset, int count)
		{
			if (buffer == null || offset < 0 || count < 0 || offset + count > buffer.Length)
				return null;

			return new CompatArraySegment<byte> (buffer, offset, count);
		}

		public void Return ()
		{
			if (smallBufferManager != null) {
				if (Header != null) {
					smallBufferManager.ReturnBuffer (Header.Value);
					Header = null;
				}
				if (Padding != null) {
					smallBufferManager.ReturnBuffer (Padding.Value);
					Padding = null;
				}
			}

			if (bigBufferManager == null || Body == null)
				return;
			bigBufferManager.ReturnBuffer (Body.Value);
			Body = null;
		}
	}
}
