/* ====================================================================
 * The Apache Software License, Version 1.1
 *
 * Authors:
 *	Daniel Lopez Ridruejo
 * 	Gonzalo Paniagua Javier
 *
 * Copyright (c) 2002 Daniel Lopez Ridruejo.
 *           (c) 2002,2003 Ximian, Inc.
 *           All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 *
 * 3. The end-user documentation included with the redistribution,
 *    if any, must include the following acknowledgment:
 *       "This product includes software developed by 
 *        Daniel Lopez Ridruejo (daniel@rawbyte.com) and
 *        Ximian Inc. (http://www.ximian.com)"
 *    Alternately, this acknowledgment may appear in the software itself,
 *    if and wherever such third-party acknowledgments normally appear.
 *
 * 4. The name "mod_mono" must not be used to endorse or promote products 
 *    derived from this software without prior written permission. For written
 *    permission, please contact daniel@rawbyte.com.
 *
 * 5. Products derived from this software may not be called "mod_mono",
 *    nor may "mod_mono" appear in their name, without prior written
 *    permission of Daniel Lopez Ridruejo and Ximian Inc.
 *
 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED.  IN NO EVENT SHALL DANIEL LOPEZ RIDRUEJO OR
 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 * ====================================================================
 *
 */
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Mono.ASPNET
{
	enum Cmd
	{
		FIRST_COMMAND,
		SEND_FROM_MEMORY = 0,
		GET_SERVER_VARIABLE,
		SET_RESPONSE_HEADER,
		GET_LOCAL_PORT,
		FLUSH,
		CLOSE,
		SHOULD_CLIENT_BLOCK,
		SETUP_CLIENT_BLOCK,
		GET_CLIENT_BLOCK,
		SET_STATUS,
		DECLINE_REQUEST,
		NOT_FOUND,
		LAST_COMMAND
	}

	public class ModMonoRequest : MarshalByRefObject
	{
		const int MAX_STRING_SIZE = 1024 * 10;
		BinaryReader reader;
		BinaryWriter writer;
		Hashtable serverVariables = new Hashtable (CaseInsensitiveHashCodeProvider.Default,
							   CaseInsensitiveComparer.Default);
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

		public ModMonoRequest (NetworkStream ns)
		{
			reader = new BinaryReader (ns);
			writer = new BinaryWriter (ns);
			GetInitialData ();
		}

		void GetInitialData ()
		{
			verb = ReadString ();
			uri = ReadString ();
			queryString = ReadString ();
			protocol = ReadString ();
			int nheaders = reader.ReadInt32 ();
			headers = new Hashtable (CaseInsensitiveHashCodeProvider.Default,
						 CaseInsensitiveComparer.Default);
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
			SendSimpleCommand (Cmd.SEND_FROM_MEMORY);
			writer.Write (length);
			writer.Write (data, position, length);
		}

		public void SetResponseHeader (string name, string value)
		{
			SendSimpleCommand (Cmd.SET_RESPONSE_HEADER);
			WriteString (name);
			WriteString (value);
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
			SendSimpleCommand (Cmd.FLUSH);
		}

		public void Close ()
		{
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

