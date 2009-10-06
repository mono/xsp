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
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#if NET_2_0
using System.Collections.Generic;
#endif

namespace Mono.WebServer
{
	enum Cmd
	{
		FIRST_COMMAND,
		SEND_FROM_MEMORY = 0,
		GET_SERVER_VARIABLES,
		SET_RESPONSE_HEADERS,
		GET_LOCAL_PORT,
		CLOSE,
		SHOULD_CLIENT_BLOCK,
		SETUP_CLIENT_BLOCK,
		GET_CLIENT_BLOCK,
		SET_STATUS,
		DECLINE_REQUEST,
		NOT_FOUND,
		IS_CONNECTED,
		SEND_FILE,
		SET_CONFIGURATION,
		LAST_COMMAND
	}

	struct ModMonoConfig
	{
		bool changed;
		bool outputBuffering;
		
		public bool Changed {
			get { return changed; }
			set { changed = value; } 
		}

		public bool OutputBuffering {
			get { return outputBuffering; }
			set {
				changed |= (value != outputBuffering);
				outputBuffering = value;
			}
		}
	}
	
	public class ModMonoRequest
	{
		const int MAX_STRING_SIZE = 1024 * 10;
		const int INITIAL_MEMORY_STREAM_SIZE = 2048;
		
		BinaryReader reader;
		BinaryWriter writer;
		MemoryStream reader_ms;
		MemoryStream writer_ms;
		bool got_server_vars;
#if NET_2_0
		Dictionary <string, string> serverVariables;
#else
		Hashtable serverVariables;
#endif
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
#if NET_2_0
		Dictionary <string, string> headers;
#else
		Hashtable headers;
#endif
		int clientBlock;
		bool shutdown;
		StringBuilder out_headers = new StringBuilder ();
		bool headers_sent;
		string physical_path;
		ModMonoConfig mod_mono_config;
		Socket client;

		public ModMonoRequest (Socket client)
		{
			mod_mono_config.OutputBuffering = true;
			this.client = client;
			reader_ms = new MemoryStream (INITIAL_MEMORY_STREAM_SIZE);
			writer_ms = new MemoryStream (INITIAL_MEMORY_STREAM_SIZE);
			reader = new BinaryReader (reader_ms);
			writer = new BinaryWriter (writer_ms);

#if NET_2_0
			serverVariables = new Dictionary <string, string> (StringComparer.OrdinalIgnoreCase);
#else
			serverVariables = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant);
#endif
			GetInitialData ();
		}

		public bool HeadersSent {
			get { return headers_sent; }
		}

		public bool ShuttingDown {
			get { return shutdown; }
		}

		void FillBuffer (int count)
		{
			if (reader_ms.Capacity < count)
				reader_ms.SetLength (count);

			// This will "reset" the stream
			reader_ms.SetLength (0);
			reader_ms.Seek (0, SeekOrigin.Begin);
			
			byte[] buffer = reader_ms.GetBuffer ();
			int received = client.Receive (buffer, count, SocketFlags.None);
			reader_ms.SetLength (received);
		}		

		void Send ()
		{
			client.Send (writer_ms.GetBuffer (), (int) writer_ms.Length, SocketFlags.None);
			writer_ms.Position = 0;
			writer_ms.SetLength (0);
		}
		
		// KEEP IN SYNC WITH mod_mono.h!!
		const byte protocol_version = 9;
		void GetInitialData ()
		{
			FillBuffer (5);
			
			byte cmd = reader.ReadByte ();
			shutdown = (cmd == 0);
			if (shutdown)
				return;

			if (cmd != protocol_version) {
				string msg = String.Format ("mod_mono and xsp have different versions. Expected '{0}', got {1}", protocol_version, cmd);
				Console.WriteLine (msg);
				Console.Error.WriteLine (msg);
				throw new InvalidOperationException (msg);
			}

			Int32 dataSize = reader.ReadInt32 ();
			FillBuffer (dataSize);
			
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
#if NET_2_0
			headers = new Dictionary <string, string> (StringComparer.OrdinalIgnoreCase);
#else
			headers = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant);
#endif
			
			for (int i = 0; i < nheaders; i++) {
				string key = ReadString ();
				if (headers.ContainsKey (key)) {
					Console.WriteLine ("WARNING: duplicate header found! Overwriting old value with the new one.");
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

			byte [] buf = new byte [size];
			string s;
			if (size != 0) {
				int chunk;
				int total = 0;
				do {
					chunk = reader.Read (buf, total, size - total);
					if (chunk == 0)
						throw new IOException ("Lost connection with mod_mono");
					if (chunk > 0)
						total += chunk;
				} while ((chunk > 0 && total < size));

				s = Encoding.UTF8.GetString (buf);
			} else {
				s = "";
			}
			return s;
		}

		void WriteString (string s)
		{
			byte [] bytes = Encoding.UTF8.GetBytes (s);
			writer.Write (bytes.Length);
			writer.Write (bytes);
		}
		
		public void Decline ()
		{
			writer.Write ((int) Cmd.DECLINE_REQUEST);
			Send ();
		}

		public void NotFound ()
		{
			writer.Write ((int) Cmd.NOT_FOUND);
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

		public void SendResponseFromMemory (byte [] data, int position, int length)
		{
			BufferConfig ();
			BufferHeaders ();
			writer.Write ((int) Cmd.SEND_FROM_MEMORY);
			writer.Write (length);
			Send ();
			client.Send (data, position, length, SocketFlags.None);
		}

		public void SendFile (string filename)
		{
			BufferConfig ();
			BufferHeaders ();
			writer.Write ((int) Cmd.SEND_FILE);
			WriteString (filename);
			Send ();
		}

		void BufferConfig ()
		{
			if (!mod_mono_config.Changed)
				return;

			mod_mono_config.Changed = false;
			writer.Write ((int) Cmd.SET_CONFIGURATION);
			writer.Write (Convert.ToByte (mod_mono_config.OutputBuffering));
		}
		
		void BufferHeaders ()
		{
			if (headers_sent)
				return;

			writer.Write ((int) Cmd.SET_RESPONSE_HEADERS);
			WriteString (out_headers.ToString ());
			out_headers = null;
			headers_sent = true;
		}

		public void SetResponseHeader (string name, string value)
		{
			if (!headers_sent)
				out_headers.AppendFormat ("{0}\0{1}\0", name, value);
		}

		public void SetOutputBuffering (bool doBuffer)
		{
			mod_mono_config.OutputBuffering = doBuffer;
		}
		
		public string [] GetAllHeaders ()
		{
			ICollection k = headers.Keys;
			string [] keys = new string [k.Count];
			k.CopyTo (keys, 0);
			return keys;
		}

		public string [] GetAllHeaderValues ()
		{
			ICollection v = headers.Values;
			string [] values = new string [v.Count];
			v.CopyTo (values, 0);
			return values;
		}

		public string GetRequestHeader (string name)
		{
			if (headers.ContainsKey (name))
				return headers [name] as string;
			return null;
		}

		void GetServerVariables ()
		{
			writer.Write ((int) Cmd.GET_SERVER_VARIABLES);
			Send ();

			FillBuffer (4);
			int blockSize = reader.ReadInt32 ();

			FillBuffer (blockSize);
			int nvars = reader.ReadInt32 ();
			while (nvars > 0) {
				string key = ReadString ();
				if (serverVariables.ContainsKey (key)) {
					Console.WriteLine ("WARNING! Duplicate server variable found. Overwriting old value with the new one.");
					serverVariables [key] = ReadString ();
				} else
					serverVariables.Add (key, ReadString ());
				
				nvars--;
			}

			got_server_vars = true;
		}

		public string GetServerVariable (string name)
		{
			if (!got_server_vars)
				GetServerVariables ();

			if (serverVariables.ContainsKey (name))
				return (string) serverVariables [name];

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

			writer.Write ((int) Cmd.GET_LOCAL_PORT);
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
			writer.Write ((int) Cmd.CLOSE);
			Send ();
		}

		public int SetupClientBlock ()
		{
			if (setupClientBlockCalled)
				return clientBlock;

			BufferConfig ();
			setupClientBlockCalled = true;
			writer.Write ((int) Cmd.SETUP_CLIENT_BLOCK);
			Send ();
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			clientBlock = i;
			return i;
		} 

		public bool IsConnected ()
		{
			writer.Write ((int) Cmd.IS_CONNECTED);
			Send ();
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			return (i != 0);
		}

		public bool ShouldClientBlock () 
		{
			writer.Write ((int) Cmd.SHOULD_CLIENT_BLOCK);
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
			writer.Write ((int) Cmd.GET_CLIENT_BLOCK);
			writer.Write (size);
			Send ();
			
			FillBuffer (4);
			int i = reader.ReadInt32 ();
			if (i == -1)
				return -1;
			
			if (i > size)
				throw new Exception ("Houston...");

			FillBuffer (i);
			return reader.Read (bytes, position, i);
		} 

		public void SetStatusCodeLine (int code, string status)
		{
			writer.Write ((int) Cmd.SET_STATUS);
			writer.Write (code);
			WriteString (status);
			Send ();
		}
	}
}

