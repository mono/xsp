//
// Mono.ASPNET.IWebSource
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
// 	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004 Novell, Inc
//


using System;
using System.Net.Sockets;

namespace Mono.ASPNET
{
	public interface IWebSource
	{
		Socket CreateSocket ();
		IWorker CreateWorker (Socket client, ApplicationServer server);
		Type GetApplicationHostType ();
		IRequestBroker CreateRequestBroker ();
	}
	
	public interface IWorker
	{
		void Run (object state);
		int Read (byte[] buffer, int position, int size);
		void Write (byte[] buffer, int position, int size);
		void Close ();
		void Flush ();
	}
}
