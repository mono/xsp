//
// DumpExtension.cs
//
// Author:
//   Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (C) Ximian, Inc. 2003
//

using System;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.IO;
using System.Net;

public class DumpExtension : SoapExtension 
{
	Stream oldStream;
	MemoryStream newStream;
	string filename = "dump.log";
	bool dump;

	public DumpExtension ()
	{
	}

	public override Stream ChainStream( Stream stream )
	{
		if (!dump) return stream;

		oldStream = stream;
		newStream = new MemoryStream ();
		return newStream;
	}

	public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute) 
	{
		return false;
	}

	public override object GetInitializer(Type webServiceType) 
	{
		if (webServiceType.GetCustomAttributes (typeof (DumpAttribute), false).Length > 0)
			return true;
		else
			return false;
	}

	public override void Initialize(object initializer) 
	{
		dump = (bool) initializer;
	}

	public override void ProcessMessage(SoapMessage message) 
	{
		if (!dump) return;

		switch (message.Stage) 
		{
			case SoapMessageStage.BeforeSerialize:
				break;
			case SoapMessageStage.AfterSerialize:
				DumpOut ();
				break;
			case SoapMessageStage.BeforeDeserialize:
				DumpIn ();
				break;
			case SoapMessageStage.AfterDeserialize:
				break;
			default:
				throw new Exception("invalid stage");
		}
	}

	public void DumpOut ()
	{
		Dump (newStream, ">> Outgoing");
		newStream.WriteTo (oldStream);
	}

	public void DumpIn ()
	{
		byte[] buffer = new byte[1000];
		int n=0;
		while ((n=oldStream.Read (buffer, 0, 1000)) > 0)
			newStream.Write (buffer, 0, n);

		newStream.Position = 0;
		Dump (newStream, ">> Incoming");
	}

	public void Dump (MemoryStream stream, string msg)
	{
		string fn = Path.Combine (Path.GetTempPath (), filename);
		FileStream fs = new FileStream (fn, FileMode.Append, FileAccess.Write);
		StreamWriter sw = new StreamWriter (fs);
		sw.WriteLine ();
		sw.WriteLine (msg);
		sw.Flush ();
		stream.WriteTo (fs);
		fs.Close ();
		stream.Position = 0;
	}
}

public class DumpAttribute: Attribute
{
}
