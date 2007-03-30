//
// ApplicationServer.cs
//
// Authors:
//      Marek Habersack (mhabersack@novell.com)
//
// Copyright (c) Copyright 2007 Novell, Inc
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
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace Mono.WebServer
{
	public sealed class WebTrace
	{
		static string GetMethodName (StackFrame sf)
		{
			MethodBase mi = sf.GetMethod ();

			if (mi != null)
				return String.Format ("{0}.{1}(): ", mi.ReflectedType, mi.Name);
			return null;
		}

		static string GetExtraInfo (StackFrame sf)
		{
#if NET_2_0
			string threadid = String.Format ("thread_id: {0}", Thread.CurrentThread.ManagedThreadId.ToString ("x"));
			string domainid = String.Format ("appdomain_id: {0}", AppDomain.CurrentDomain.Id.ToString ("x"));
#else
			string threadid = String.Format ("thread_hash: {0}", Thread.CurrentThread.GetHashCode ().ToString ("x"));
			string domainid = String.Format ("appdomain_hash: {0}", AppDomain.CurrentDomain.GetHashCode ().ToString ("x"));
#endif
			
			string filepath = sf != null ? sf.GetFileName () : null;
			if (filepath != null && filepath.Length > 0)
				return String.Format (" [{0}, {1}, in {2}:{3}]", domainid, threadid, filepath, sf.GetFileLineNumber ());
			else
				return String.Format (" [{0}, {1}]", domainid, threadid);
		}

		static string GetExtraInfo ()
		{
			return GetExtraInfo (null);
		}

		static void Enter (string format, StackFrame sf, params object[] parms)
		{
			StringBuilder sb = new StringBuilder ("Enter: ");
					
			string methodName = GetMethodName (sf);
			if (methodName != null)
				sb.Append (methodName);
			if (format != null)
				sb.AppendFormat (format, parms);
			sb.Append (GetExtraInfo (sf));
			
			Trace.WriteLine (sb.ToString ());
			Trace.Indent ();
		}
		
		[Conditional ("WEBTRACE")]
		public static void Enter (string format, params object[] parms)
		{
			Enter (format, new StackFrame (1), parms);
		}

		[Conditional ("WEBTRACE")]
		public static void Enter ()
		{
			Enter (null, new StackFrame (1), null);
		}

		static void Leave (string format, StackFrame sf, params object[] parms)
		{
			StringBuilder sb = new StringBuilder ("Leave: ");

			string methodName = GetMethodName (sf);
			if (methodName != null)
				sb.Append (methodName);
			if (format != null)
				sb.AppendFormat (format, parms);
			sb.Append (GetExtraInfo (sf));
			
			Trace.Unindent ();
			Trace.WriteLine (sb.ToString ());
		}
		
		[Conditional ("WEBTRACE")]
		public static void Leave (string format, params object[] parms)
		{
			Leave (format, new StackFrame (1), parms);
		}

		[Conditional ("WEBTRACE")]
		public static void Leave ()
		{
			Leave (null, new StackFrame (1), null);
		}

		[Conditional ("WEBTRACE")]
		public static void WriteLine (string format, params object[] parms)
		{
			if (format == null)
				return;
			
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat (format, parms);
			sb.Append (GetExtraInfo ());
			Trace.WriteLine (sb.ToString ());
		}

		[Conditional ("WEBTRACE")]
		public static void WriteLineIf (bool cond, string format, params object[] parms)
		{
			if (!cond)
				return;
			
			if (format == null)
				return;
			
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat (format, parms);
			sb.Append (GetExtraInfo ());
			Trace.WriteLine (sb.ToString ());
		}
	}
}
