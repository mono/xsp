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
using System.Text;

namespace Mono.ASPNET
{
	enum Cmd
	{
		FIRST_COMMAND,
		GET_PROTOCOL = 0,
		GET_METHOD,
		SEND_FROM_MEMORY,
		GET_PATH_INFO,
		GET_SERVER_VARIABLE,
		GET_PATH_TRANSLATED,
		GET_SERVER_PORT,
		SET_RESPONSE_HEADER,
		GET_REQUEST_HEADER,
		GET_FILENAME,
		GET_URI,
		GET_QUERY_STRING,
		GET_REMOTE_ADDRESS,
		GET_LOCAL_ADDRESS,
		GET_REMOTE_PORT,
		GET_LOCAL_PORT,
		GET_REMOTE_NAME,
		FLUSH,
		CLOSE,
		SHOULD_CLIENT_BLOCK,
		SETUP_CLIENT_BLOCK,
		GET_CLIENT_BLOCK,
		SET_STATUS_LINE,
		SET_STATUS_CODE,
		DECLINE_REQUEST,
		LAST_COMMAND
	}

	public class ModMonoRequest : MarshalByRefObject
	{
		BinaryReader reader;
		BinaryWriter writer;
		NetworkStream st;

		public ModMonoRequest (NetworkStream ns)
		{
			st = ns;
			reader = new BinaryReader (st);
			writer = new BinaryWriter (st);
		}

		void SendSimpleCommand (Cmd cmd)
		{
			int b = (int) cmd;
			writer.Write (b);
		}

		void ReadEnd ()
		{
			byte b = reader.ReadByte ();
			if (b != 0)
				throw new Exception ("Protocol violation or error");
		}

		string ReadString ()
		{
			int size = reader.ReadInt32 ();
			byte [] buf = new byte [size];
			string s;
			if (size != 0) {
				st.Read (buf, 0, size);
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
			ReadEnd ();
		}

		public string GetProtocol ()
		{
			SendSimpleCommand (Cmd.GET_PROTOCOL);
			ReadEnd ();
			return ReadString ();
		}

		public string GetHttpVerbName ()
		{
			SendSimpleCommand (Cmd.GET_METHOD);
			ReadEnd ();
			return ReadString ();
		}

		public void SendResponseFromMemory (byte [] data, int length)
		{
			SendSimpleCommand (Cmd.SEND_FROM_MEMORY);
			writer.Write (length);
			st.Write (data, 0, length);
			ReadEnd ();
		}

		public void SetResponseHeader (string name, string value)
		{
			SendSimpleCommand (Cmd.SET_RESPONSE_HEADER);
			WriteString (name);
			WriteString (value);
			ReadEnd ();
		}

		public string GetRequestHeader (string name)
		{
			SendSimpleCommand (Cmd.GET_REQUEST_HEADER);
			WriteString (name);
			ReadEnd ();
			return ReadString ();
		}

		public string GetServerVariable (string name)
		{
			SendSimpleCommand (Cmd.GET_SERVER_VARIABLE);
			WriteString (name);
			ReadEnd ();
			return ReadString ();
		}

		public string GetUri ()
		{
			SendSimpleCommand (Cmd.GET_URI);
			ReadEnd ();
			return ReadString ();
		}

		public string GetFileName ()
		{
			SendSimpleCommand (Cmd.GET_FILENAME);
			ReadEnd ();
			return ReadString ();
		}

		public string GetQueryString ()
		{
			SendSimpleCommand (Cmd.GET_QUERY_STRING);
			ReadEnd ();
			return ReadString ();
		}

		// May be different from Connection.GetLocalPort depending on Apache configuration,
		// for things like self referential URLs, etc.

		public int GetServerPort ()
		{
			SendSimpleCommand (Cmd.GET_SERVER_PORT);
			ReadEnd ();
			return reader.ReadInt32 ();
		}

		public string GetRemoteAddress ()
		{
			SendSimpleCommand (Cmd.GET_REMOTE_ADDRESS);
			ReadEnd ();
			return ReadString ();
		}

		public string GetRemoteName ()
		{
			SendSimpleCommand (Cmd.GET_REMOTE_NAME);
			ReadEnd ();
			return ReadString ();
		}

		public string GetLocalAddress ()
		{
			SendSimpleCommand (Cmd.GET_LOCAL_ADDRESS);
			ReadEnd ();
			return ReadString ();
		}

		public int GetLocalPort ()
		{
			SendSimpleCommand (Cmd.GET_LOCAL_PORT);
			ReadEnd ();
			return reader.ReadInt32 ();
		}

		public int GetRemotePort ()
		{
			SendSimpleCommand (Cmd.GET_REMOTE_PORT);
			ReadEnd ();
			return reader.ReadInt32 ();
		}

		public void Flush ()
		{
			SendSimpleCommand (Cmd.FLUSH);
			ReadEnd ();
		}

		public void Close ()
		{
			SendSimpleCommand (Cmd.CLOSE);
			ReadEnd ();
		}

		public int SetupClientBlock ()
		{
			SendSimpleCommand (Cmd.SETUP_CLIENT_BLOCK);
			ReadEnd ();
			int i = reader.ReadInt32 ();
			return i;
		} 

		public bool ShouldClientBlock() 
		{
			SendSimpleCommand (Cmd.SHOULD_CLIENT_BLOCK);
			ReadEnd ();
			int i = reader.ReadInt32 ();
			return (i != 0);
		} 

		public int GetClientBlock (byte [] bytes, int size) 
		{
			SendSimpleCommand (Cmd.GET_CLIENT_BLOCK);
			writer.Write (size);
			ReadEnd ();
			int i = reader.ReadInt32 ();
			if (i > size)
				throw new Exception ("Houston...");

			st.Read (bytes, 0, i);
			return i;
		} 

		public void SetStatusCode (int code) 
		{
			SendSimpleCommand (Cmd.SET_STATUS_CODE);
			writer.Write (code);
			ReadEnd ();
		}

		public void SetStatusLine (string status)
		{
			SendSimpleCommand (Cmd.SET_STATUS_LINE);
			WriteString (status);
			ReadEnd ();
		}
	}
}

