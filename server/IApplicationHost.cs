//
// Mono.ASPNET.MonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
//

using System;
using System.Web;

namespace Mono.ASPNET
{
	public interface IApplicationHost
	{
		string Path { get; }	
		string VPath { get; }	
		AppDomain Domain { get; }	
		IRequestBroker RequestBroker { get; set; }
	}
	
	public interface IRequestBroker
	{
	}
}

