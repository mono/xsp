//
// ModMonoRequest.cs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
// (C) Copyright 2004-2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Mono.WebServer.Apache;
using Mono.WebServer.Log;

namespace Mono.WebServer
{
	public class ModMonoRequest : IDisposable
	{
		const int MAX_STRING_SIZE = 1024 * 10;
		const int INITIAL_MEMORY_STREAM_SIZE = 1024 * 2;
		const int MAX_MEMORY_STREAM_SIZE = 1024 * 128;
		
		readonly BinaryReader reader;
		readonly BinaryWriter writer;
		readonly MemoryStream reader_ms;
		readonly MemoryStream writer_ms;
		byte[] fill_buffer;
		bool got_server_vars;
		readonly Dictionary <string, string> serverVariables;
		string verb;
		string queryString;
		string protocol;
		string uri;
		string vServerName;
		string localAddress;
		string remoteAddress;
		string remoteName;
		int localPort;
		int remotePort;
		int serverPort;
		bool setupClientBlockCalled;
		Dictionary <string, string> headers;
		int clientBlock;
		StringBuilder out_headers = new StringBuilder ();
		string physical_path;
		ModMonoConfig mod_mono_config;
		readonly Socket client;
		readonly static bool use_libc;

		public bool HeadersSent { get; private set; }

		public bool ShuttingDown { get; private set; }

		static ModMonoRequest ()
		{
			if (Environment.GetEnvironmentVariable ("XSP_NO_LIBC") != null)
				return;

			try {
				string os = File.ReadAllText ("/proc/sys/kernel/ostype");
				use_libc = os.StartsWith ("Linux");
			} catch {
			}
		}

		public ModMonoRequest (Socket client)
		{
			mod_mono_config.OutputBuffering = true;
			this.client = client;
			reader_ms = new MemoryStream (INITIAL_MEMORY_STREAM_SIZE);
			writer_ms = new MemoryStream (INITIAL_MEMORY_STREAM_SIZE);
			reader = new BinaryReader (reader_ms);
			writer = new BinaryWriter (writer_ms);
			serverVariables = new Dictionary <string, string> (StringComparer.OrdinalIgnoreCase);
			GetInitialData ();
		}

		static void Dispose (Action disposer, string name)
		{
			if (disposer == null)
				return;

			try {
				disposer ();
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "While disposing ModMonoRequest. {0} disposing failed with exception:", name);
				Logger.Write (ex);
			}
		}
		
		public void Dispose ()
		{
			fill_buffer = null;
			Dispose (() => {
				if (headers != null)
					headers.Clear ();
			}, "headers");
			
			Dispose (() => {
				if (reader != null)
					((IDisposable)reader).Dispose ();
			}, "reader");
			
			Dispose (() => {
				if (reader_ms != null)
					reader_ms.Dispose ();
			}, "reader_ms");

			Dispose (() => {
				if (writer != null)
					((IDisposable)writer).Dispose ();
			}, "writer");

			Dispose (() => {
				if (writer_ms != null)
					writer_ms.Dispose ();
			}, "writer_ms");
			
			GC.SuppressFinalize (this);
		}
		
		void FillBuffer (uint count)
		{
			if (count == 0)
				return;
			
			// This will "reset" the stream
			reader_ms.SetLength (0);
			reader_ms.Position = 0;

			var read_count = (int)count;
			int fill_buffer_length = fill_buffer == null ? 0 : fill_buffer.Length;
			if ((uint)fill_buffer_length < count) {
				if (fill_buffer == null && count <= INITIAL_MEMORY_STREAM_SIZE) {
					// Use slightly more memory initially, but save on time.
					fill_buffer = new byte [INITIAL_MEMORY_STREAM_SIZE];
				} else if (fill_buffer_length < MAX_MEMORY_STREAM_SIZE) {
					fill_buffer = new byte [System.Math.Min (count, MAX_MEMORY_STREAM_SIZE)];
					read_count = fill_buffer.Length;
				} else
					read_count = fill_buffer_length;
			}

			int total_received = 0;
			int received;
			do {
				received = client.Receive (fill_buffer, read_count, SocketFlags.None);
				total_received += received;
			} while (received == read_count && total_received < count && client.Available > 0);
			
			reader_ms.Write (fill_buffer, 0, received);
			reader_ms.Seek (0, SeekOrigin.Begin);
		}

		void Send ()
		{
			int sent = client.Send (writer_ms.GetBuffer (), (int) writer_ms.Length, SocketFlags.None);
			if (sent != (int) writer_ms.Length)
				throw new IOException ("Blocking send did not send entire buffer");
			writer_ms.Position = 0;
			writer_ms.SetLength (0);
		}

		// KEEP IN SYNC WITH mod_mono.h!!
		const byte PROTOCOL_VERSION = 9;
		void GetInitialData ()
		{
			FillBuffer (5);
			
			byte cmd = reader.ReadByte ();
			ShuttingDown = (cmd == 0);
			if (ShuttingDown) {
				Logger.Write (LogLevel.Notice, "mod-mono-server received a shutdown message");
				return;
			}

			if (cmd != PROTOCOL_VERSION) {
				string msg = String.Format ("mod_mono and xsp have different versions. Expected '{0}', got {1}", PROTOCOL_VERSION, cmd);
				Logger.Write (LogLevel.Error, msg);
				throw new InvalidOperationException (msg);
			}

			Int32 dataSize = reader.ReadInt32 ();
			FillBuffer ((uint)dataSize);
			
			verb = ReadString ();
			vServerName = ReadString ();
			uri = ReadString ();
			queryString = ReadString ();
			protocol = ReadString ();
			localAddress = ReadString ();
			serverPort = reader.ReadInt32 ();
			remoteAddress = ReadString ();
			remotePort = reader.ReadInt32 ();
			remoteName = ReadString ();
			reader.ReadInt32 (); // This is autoApp!!! (unused!?
			int nheaders = reader.ReadInt32 ();
			headers = new Dictionary <string, string> (StringComparer.OrdinalIgnoreCase);
			
			for (int i = 0; i < nheaders; i++) {
				string key = ReadString ();
				if (String.IsNullOrEmpty (key))
					continue;
				
				if (headers.ContainsKey (key)) {
					Logger.Write (LogLevel.Warning, "Duplicate header '{0}' found! Overwriting old value with the new one.", key);
					headers [key] = ReadString ();
				} else
					headers.Add (key, ReadString ());
			}

			if (reader.ReadByte () != 0)
				physical_path = ReadString ();
		}

		string ReadString ()
		{
			int size = reader.ReadInt32 ();
			if (size < 0 || size > MAX_STRING_SIZE)
				throw new ArgumentOutOfRangeException ("size", "Abnormal string size " + size);

			var buf = new byte [size];
			if (size == 0)
				return String.Empty;
			int chunk;
			int total = 0;
			do {
				chunk = reader.Read (buf, total, size - total);
				if (chunk == 0)
					throw new IOException ("Lost connection with mod_mono");
				if (chunk > 0)
					total += chunk;
			} while ((chunk > 0 && total < size));
			return Encoding.UTF8.GetString (buf);
		}

		void WriteString (string s)
		{
			byte [] bytes = Encoding.UTF8.GetBytes (s);
			writer.Write (bytes.Length);
			writer.Write (bytes);
		}
		
		public void Decline ()
		{
			writer.Write ((int) ModMonoCmd.DECLINE_REQUEST);
			Send ();
		}

		public void NotFound ()
		{
			writer.Write ((int) ModMonoCmd.NOT_FOUND);
			Send ();
		}

		public string GetProtocol ()
		{
			return protocol;
		}

		public string GetHttpVerbName ()
		{
			return verb;
		}

		public void SendResponseFromMemory (IntPtr ptr, int length)
		{
			BufferConfig ();
			BufferHeaders ();
			writer.Write ((int) ModMonoCmd.SEND_FROM_MEMORY);
			writer.Write (length);
			Send ();
			if (use_libc) {
				int sent = Send (ptr, length);
				if (sent != length)
					throw new IOException ("Blocking send did not send entire buffer");
				return;
			}
			
			while (length > 0) {
				if (writer_ms.Position != 0)
					throw new Exception ("Should not happen... We called Send()!");

				int size = System.Math.Min (16384, length);
				if (writer_ms.Capacity < size)
					writer_ms.Capacity = size;

				byte [] buf = writer_ms.GetBuffer ();
				Marshal.Copy (ptr, buf, 0, size);
				length -= size;
				unsafe { ptr = (IntPtr)((byte*)ptr.ToPointer () + size); }
				int sent = client.Send (buf, size, SocketFlags.None);
				if (sent != size)
					throw new IOException ("Blocking send did not send entire buffer");
			}
		}

		// FIXME: the return value is never used
		unsafe int Send (IntPtr ptr, int len)
		{
			int total = 0;
			var bptr = (byte *) ptr.ToPointer ();
			while (total < len) {
				// 0x4000 no sigpipe
				int n = send_libc (client.Handle.ToInt32 (), bptr + total, (IntPtr) (len - total), 0x4000);
				if (n >= 0) {
					total += n;
				} else if (Marshal.GetLastWin32Error () != 4 /* EINTR */) {
					throw new IOException ();
				}
			}

			return total;
		}

		[DllImport ("libc", SetLastError=true, EntryPoint="send")]
		unsafe extern static int send_libc (int s, byte *buffer, IntPtr len, int flags);

		public void SendResponseFromMemory (byte [] data, int position, int length)
		{
			BufferConfig ();
			BufferHeaders ();
			writer.Write ((int) ModMonoCmd.SEND_FROM_MEMORY);
			writer.Write (length);
			Send ();
			int sent = client.Send (data, position, length, SocketFlags.None);
			if (sent != length)
				throw new IOException ("Blocking send did not send entire buffer");
		}

		public void SendFile (string filename)
		{
			BufferConfig ();
			BufferHeaders ();
			writer.Write ((int) ModMonoCmd.SEND_FILE);
			WriteString (filename);
			Send ();
		}

		void BufferConfig ()
		{
			if (!mod_mono_config.Changed)
				return;

			mod_mono_config.Changed = false;
			writer.Write ((int) ModMonoCmd.SET_CONFIGURATION);
			writer.Write (Convert.ToByte (mod_mono_config.OutputBuffering));
		}
		
		void BufferHeaders ()
		{
			if (HeadersSent)
				return;

			writer.Write ((int) ModMonoCmd.SET_RESPONSE_HEADERS);
			WriteString (out_headers.ToString ());
			out_headers = null;
			HeadersSent = true;
		}

		public void SetResponseHeader (string name, string value)
		{
			if (!HeadersSent)
				out_headers.AppendFormat ("{0}\0{1}\0", name, value);
		}

		public void SetOutputBuffering (bool doBuffer)
		{
			mod_mono_config.OutputBuffering = doBuffer;
		}
		
		public string [] GetAllHeaders ()
		{
			ICollection k = headers.Keys;
			var keys = new string [k.Count];
			k.CopyTo (keys, 0);
			return keys;
		}

		public string [] GetAllHeaderValues ()
		{
			ICollection v = headers.Values;
			var values = new string [v.Count];
			v.CopyTo (values, 0);
			return values;
		}

		public string GetRequestHeader (string name)
		{
			if (headers.ContainsKey (name))
				return headers [name];
			return null;
		}

		void GetServerVariables ()
		{
			writer.Write ((int) ModMonoCmd.GET_SERVER_VARIABLES);
			Send ();

			FillBuffer (4);
			int blockSize = reader.ReadInt32 ();
			
			FillBuffer ((uint)blockSize);
			int nvars = reader.ReadInt32 ();
			while (nvars > 0) {
				string key = ReadString ();
				nvars--;
				if (String.IsNullOrEmpty (key))
					continue;
				
				if (serverVariables.ContainsKey (key)) {
					Logger.Write(LogLevel.Warning, "Duplicate server variable '{0}' found. Overwriting old value with the new one.", key);
					serverVariables [key] = ReadString ();
				} else
					serverVariables.Add (key, ReadString ());
			}

			got_server_vars = true;
		}

		public string GetServerVariable (string name)
		{
			if (!got_server_vars)
				GetServerVariables ();

			if (serverVariables.ContainsKey (name))
				return serverVariables [name];

			return null;
		}

		public string GetPhysicalPath ()
		{
			return physical_path;
		}

		public string GetUri ()
		{
			return uri;
		}

		public string GetVirtualServerName ()
		{
			return vServerName;
		}

		public string GetQueryString ()
		{
			return queryString;
		}

		// May be different from Connection.GetLocalPort depending on Apache configuration,
		// for things like self referential URLs, etc.
		public int GetServerPort ()
		{
			return serverPort;
		}

		public string GetRemoteAddress ()
		{
			return remoteAddress;
		}

		public string GetRemoteName ()
		{
			return remoteName;
		}

		public string GetLocalAddress ()
		{
			return localAddress;
		}

		public int GetLocalPort ()
		{
			if (localPort != 0)
				return localPort;

			writer.Write ((int) ModMonoCmd.GET_LOCAL_PORT);
			Send ();
			FillBuffer (4);
			localPort = reader.ReadInt32 ();
			return localPort;
		}

		public int GetRemotePort ()
		{
			return remotePort;
		}

		public void Close ()
		{
			BufferHeaders ();
			writer.Write ((int) ModMonoCmd.CLOSE);
			Send ();
		}

		public int SetupClientBlock ()
		{
			if (setupClientBlockCalled)
				return clientBlock;

			BufferConfig ();
			setupClientBlockCalled = true;
			writer.Write ((int) ModMonoCmd.SETUP_CLIENT_BLOCK);
			Send ();
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			clientBlock = i;
			return i;
		} 

		public bool IsConnected ()
		{
			writer.Write ((int) ModMonoCmd.IS_CONNECTED);
			Send ();
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			return (i != 0);
		}

		public bool ShouldClientBlock () 
		{
			writer.Write ((int) ModMonoCmd.SHOULD_CLIENT_BLOCK);
			Send ();
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			return (i == 0);
		} 

		public int GetClientBlock ([Out] byte [] bytes, int position, int size) 
		{
			if (SetupClientBlock () != 0) return 0;

			/*
			 * turns out that that GET_CLIENT_BLOCK (ap_get_client_block) can
			 * return -1 if a socket is closed
			 */
			writer.Write ((int) ModMonoCmd.GET_CLIENT_BLOCK);
			writer.Write (size);
			Send ();
			
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			if (i == -1)
				return -1;
			
			if (i > size)
				throw new Exception ("Houston...");

			FillBuffer ((uint)i);
			return reader.Read (bytes, position, i);
		} 

		public void SetStatusCodeLine (int code, string status)
		{
			writer.Write ((int) ModMonoCmd.SET_STATUS);
			writer.Write (code);
			WriteString (status);
			Send ();
		}
	}
}

