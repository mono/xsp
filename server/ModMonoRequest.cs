/* ====================================================================
 * The Apache Software License, Version 1.1
 *
 * Authors:
 *	Daniel Lopez Ridruejo
 * 	Gonzalo Paniagua Javier
 *
 * Copyright (c) 2002 Daniel Lopez Ridruejo.
 *           (c) 2002 Ximian, Inc.
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
		ALIAS_MATCHES,
		LAST_COMMAND
	}

	public class ModMonoRequest
	{
		Socket sock;
		BinaryReader reader;
		BinaryWriter writer;
		NetworkStream st;

		public ModMonoRequest (Socket sock)
		{
			this.sock = sock;
			st = new NetworkStream (sock, false);
			reader = new BinaryReader (st);
			writer = new BinaryWriter (st);
		}

		[Conditional("DEBUG")]
		static void WriteDebug (bool yeah, string format, params object [] args)
		{
			Console.WriteLine (format, args);
		}
		
		[Conditional("DEBUG")]
		static void WriteDebug (string format, params object [] args)
		{
		}
		
		void SendSimpleCommand (Cmd cmd)
		{
			WriteDebug ("SendSimpleCommand -> Available: {0}", sock.Available);
			int b = (int) cmd;
			WriteDebug ("Escribo cmd: {0} {1}", b, cmd);
			writer.Write (b);
		}

		void ReadEnd ()
		{
			WriteDebug ("ReadEnd");
			byte b = reader.ReadByte ();
			WriteDebug ("ReadEnd done");
			if (b != 0) {
				WriteDebug ("b es :{0} {1}", b, (char) b);
				throw new Exception ("Protocol violation or error");
			}
		}

		string ReadString ()
		{
			WriteDebug ("ReadString -> Available: {0}", sock.Available);
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
			WriteDebug ("Leo string. Size: {0} {1}", s.Length, s);
			return s;
		}

		void WriteString (string s)
		{
			WriteDebug ("WriteString -> Available: {0}", sock.Available);
			WriteDebug ("envio string: {0} {1}", s.Length, s);
			writer.Write (Encoding.Default.GetByteCount (s));
			writer.Write (Encoding.Default.GetBytes (s));
			WriteDebug ("Enviada!");
		}
		
		public string GetProtocol ()
		{
			WriteDebug ("GetProtocol");
			SendSimpleCommand (Cmd.GET_PROTOCOL);
			ReadEnd ();
			return ReadString ();
		}

		public string GetHttpVerbName ()
		{
			WriteDebug ("GetHttpVerbName");
			SendSimpleCommand (Cmd.GET_METHOD);
			ReadEnd ();
			return ReadString ();
		}

		public void SendResponseFromMemory (byte [] data, int length)
		{
			WriteDebug ("SendResponseFromMemory");
			SendSimpleCommand (Cmd.SEND_FROM_MEMORY);
			writer.Write (length);
			st.Write (data, 0, length);
			ReadEnd ();
		}

		public void SetResponseHeader (string name, string value)
		{
			WriteDebug ("SetResponseHeader ({0} = {1})", name, value);
			SendSimpleCommand (Cmd.SET_RESPONSE_HEADER);
			WriteString (name);
			WriteString (value);
			ReadEnd ();
		}

		public string GetRequestHeader (string name)
		{
			WriteDebug ("GetRequestHeader ({0})", name);
			SendSimpleCommand (Cmd.GET_REQUEST_HEADER);
			WriteString (name);
			ReadEnd ();
			return ReadString ();
		}

		public string RemovePrefix (string uri, string appPrefix)
		{
			WriteDebug ("RemovePrefix (uri = {0}, appPrefix = {1})", uri, appPrefix);
			if (uri == appPrefix)
				return "/";

			SendSimpleCommand (Cmd.ALIAS_MATCHES);
			WriteString (uri);
			WriteString (appPrefix);
			ReadEnd ();
			int l = reader.ReadInt32 ();
			if (l == 0)
				return uri;

			return uri.Substring (l);
		}

		public string GetServerVariable (string name)
		{
			WriteDebug ("GetServerVariable ({0})", name);
			SendSimpleCommand (Cmd.GET_SERVER_VARIABLE);
			WriteString (name);
			ReadEnd ();
			return ReadString ();
		}

		public string GetUri ()
		{
			WriteDebug ("GetUri");
			SendSimpleCommand (Cmd.GET_URI);
			ReadEnd ();
			return ReadString ();
		}

		public string GetFileName ()
		{
			WriteDebug ("GetFileName");
			SendSimpleCommand (Cmd.GET_FILENAME);
			ReadEnd ();
			return ReadString ();
		}

		public string GetQueryString ()
		{
			WriteDebug ("GetQueryString");
			SendSimpleCommand (Cmd.GET_QUERY_STRING);
			ReadEnd ();
			return ReadString ();
		}

		// May be different from Connection.GetLocalPort depending on Apache configuration,
		// for things like self referential URLs, etc.

		public int GetServerPort ()
		{
			WriteDebug ("GetServerPort");
			SendSimpleCommand (Cmd.GET_SERVER_PORT);
			ReadEnd ();
			return reader.ReadInt32 ();
		}

		public string GetRemoteAddress ()
		{
			WriteDebug ("GetRemoteAddress");
			SendSimpleCommand (Cmd.GET_REMOTE_ADDRESS);
			ReadEnd ();
			return ReadString ();
		}

		public string GetRemoteName ()
		{
			WriteDebug ("GetRemoteName");
			SendSimpleCommand (Cmd.GET_REMOTE_NAME);
			ReadEnd ();
			return ReadString ();
		}

		public string GetLocalAddress ()
		{
			WriteDebug ("GetLocalAddress");
			SendSimpleCommand (Cmd.GET_LOCAL_ADDRESS);
			ReadEnd ();
			return ReadString ();
		}

		public int GetLocalPort ()
		{
			WriteDebug ("GetLocalPort");
			SendSimpleCommand (Cmd.GET_LOCAL_PORT);
			ReadEnd ();
			return reader.ReadInt32 ();
		}

		public int GetRemotePort ()
		{
			WriteDebug ("GetRemotePort");
			SendSimpleCommand (Cmd.GET_REMOTE_PORT);
			ReadEnd ();
			return reader.ReadInt32 ();
		}

		public void Flush ()
		{
			WriteDebug ("Flush");
			SendSimpleCommand (Cmd.FLUSH);
			ReadEnd ();
		}

		public void Close ()
		{
			WriteDebug ("Close");
			SendSimpleCommand (Cmd.CLOSE);
			ReadEnd ();
		}

		public int SetupClientBlock ()
		{
			WriteDebug ("SetupClientBlock");
			SendSimpleCommand (Cmd.SETUP_CLIENT_BLOCK);
			ReadEnd ();
			int i = reader.ReadInt32 ();
			return i;
		} 

		public bool ShouldClientBlock() 
		{
			WriteDebug ("ShouldClientBlock");
			SendSimpleCommand (Cmd.SHOULD_CLIENT_BLOCK);
			ReadEnd ();
			int i = reader.ReadInt32 ();
			return (i != 0);
		} 

		public int GetClientBlock (byte [] bytes, int size) 
		{
			WriteDebug ("GetClientBlock");
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
			WriteDebug ("SetStatusCode");
			SendSimpleCommand (Cmd.SET_STATUS_CODE);
			writer.Write (code);
			ReadEnd ();
			WriteDebug ("END SetStatusCode");
		}

		public void SetStatusLine (string status)
		{
			WriteDebug ("SetStatusLine");
			SendSimpleCommand (Cmd.SET_STATUS_LINE);
			WriteString (status);
			ReadEnd ();
			WriteDebug ("END SetStatusLine");
		}
	}
}

