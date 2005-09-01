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
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
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

namespace Mono.WebServer
{
	enum Cmd
	{
		FIRST_COMMAND,
		SEND_FROM_MEMORY = 0,
		GET_SERVER_VARIABLE,
		SET_RESPONSE_HEADERS,
		GET_LOCAL_PORT,
		FLUSH,
		CLOSE,
		SHOULD_CLIENT_BLOCK,
		SETUP_CLIENT_BLOCK,
		GET_CLIENT_BLOCK,
		SET_STATUS,
		DECLINE_REQUEST,
		NOT_FOUND,
		IS_CONNECTED,
		SEND_FILE,
		LAST_COMMAND
	}

	public class ModMonoRequest
	{
		const int MAX_STRING_SIZE = 1024 * 10;
		BinaryReader reader;
		BinaryWriter writer;
		Hashtable serverVariables = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant,
							   CaseInsensitiveComparer.DefaultInvariant);
		string verb;
		string queryString;
		string protocol;
		string uri;
		string localAddress;
		string remoteAddress;
		string remoteName;
		int localPort;
		int remotePort;
		int serverPort;
		bool setupClientBlockCalled;
		Hashtable headers;
		int clientBlock;
		bool shutdown;
		StringBuilder out_headers = new StringBuilder ();
		bool headers_sent;

		public ModMonoRequest (NetworkStream ns)
		{
			reader = new BinaryReader (ns);
			writer = new BinaryWriter (ns);
			GetInitialData ();
		}

		public bool ShuttingDown {
			get { return shutdown; }
		}
		
		void GetInitialData ()
		{
			byte cmd = reader.ReadByte ();
			shutdown = (cmd == 0);
			if (shutdown)
				return;

			if (cmd != 4) {
				string msg = "mod_mono and xsp have different versions.";
				Console.WriteLine (msg);
				Console.Error.WriteLine (msg);
				throw new InvalidOperationException (msg);
			}

			verb = ReadString ();
			uri = ReadString ();
			queryString = ReadString ();
			protocol = ReadString ();
			int nheaders = reader.ReadInt32 ();
			headers = new Hashtable (CaseInsensitiveHashCodeProvider.DefaultInvariant,
						 CaseInsensitiveComparer.DefaultInvariant);
			for (int i = 0; i < nheaders; i++) {
				string key = ReadString ();
				headers [key] = ReadString ();
			}

			localAddress = ReadString ();
			serverPort = reader.ReadInt32 ();
			remoteAddress = ReadString ();
			remotePort = reader.ReadInt32 ();
			remoteName = ReadString ();
		}

		void SendSimpleCommand (Cmd cmd)
		{
			int b = (int) cmd;
			writer.Write (b);
		}

		string ReadString ()
		{
			int size = reader.ReadInt32 ();
			if (size < 0 || size > MAX_STRING_SIZE)
				throw new ArgumentOutOfRangeException ("size", "Abnormal string size.");

			byte [] buf = new byte [size];
			string s;
			if (size != 0) {
				reader.Read (buf, 0, size);
				//FIXME: encoding!
				s = Encoding.Default.GetString (buf);
			} else {
				s = "";
			}
			return s;
		}

		void WriteString (string s)
		{
			writer.Write (Encoding.Default.GetByteCount (s));
			writer.Write (Encoding.Default.GetBytes (s));
		}
		
		public void Decline ()
		{
			SendSimpleCommand (Cmd.DECLINE_REQUEST);
		}

		public void NotFound ()
		{
			SendSimpleCommand (Cmd.NOT_FOUND);
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
			if (!headers_sent)
				SendHeaders ();
			SendSimpleCommand (Cmd.SEND_FROM_MEMORY);
			writer.Write (length);
			writer.Write (data, position, length);
		}

		public void SendFile (string filename)
		{
			if (!headers_sent)
				SendHeaders ();
			SendSimpleCommand (Cmd.SEND_FILE);
			WriteString (filename);
		}

		void SendHeaders ()
		{
			SendSimpleCommand (Cmd.SET_RESPONSE_HEADERS);
			WriteString (out_headers.ToString ());
			out_headers = null;
			headers_sent = true;
		}

		public void SetResponseHeader (string name, string value)
		{
			out_headers.AppendFormat ("{0}\0{1}\0", name, value);
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
			return headers [name] as string;
		}

		public string GetServerVariable (string name)
		{
			object o = serverVariables [name];
			if (o != null)
				return (string) o;

			SendSimpleCommand (Cmd.GET_SERVER_VARIABLE);
			WriteString (name);
			o = ReadString ();
			serverVariables [name] = o;

			return (string) o;
		}

		public string GetUri ()
		{
			return uri;
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

			SendSimpleCommand (Cmd.GET_LOCAL_PORT);
			localPort = reader.ReadInt32 ();
			return localPort;
		}

		public int GetRemotePort ()
		{
			return remotePort;
		}

		public void Flush ()
		{
			// No-op in mod_mono. Not needed.
			// SendSimpleCommand (Cmd.FLUSH);
		}

		public void Close ()
		{
			if (!headers_sent)
				SendHeaders ();
			SendSimpleCommand (Cmd.CLOSE);
		}

		public int SetupClientBlock ()
		{
			if (setupClientBlockCalled)
				return clientBlock;

			setupClientBlockCalled = true;
			SendSimpleCommand (Cmd.SETUP_CLIENT_BLOCK);
			int i = reader.ReadInt32 ();
			clientBlock = i;
			return i;
		} 

		public bool IsConnected ()
		{
			SendSimpleCommand (Cmd.IS_CONNECTED);
			int i = reader.ReadInt32 ();
			return (i != 0);
		}

		public bool ShouldClientBlock () 
		{
			SendSimpleCommand (Cmd.SHOULD_CLIENT_BLOCK);
			int i = reader.ReadInt32 ();
			return (i == 0);
		} 

		public int GetClientBlock ([Out] byte [] bytes, int position, int size) 
		{
			if (!ShouldClientBlock ()) return 0;
			if (SetupClientBlock () != 0) return 0;
			
			SendSimpleCommand (Cmd.GET_CLIENT_BLOCK);
			writer.Write (size);
			int i = reader.ReadInt32 ();
			if (i > size)
				throw new Exception ("Houston...");

			return reader.Read (bytes, position, i);
		} 

		public void SetStatusCodeLine (int code, string status)
		{
			SendSimpleCommand (Cmd.SET_STATUS);
			writer.Write (code);
			WriteString (status);
		}
	}
}

