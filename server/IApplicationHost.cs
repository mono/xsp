//
// Mono.ASPNET.MonoApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2003 Ximian, Inc (http://www.ximian.com)
//
namespace Mono.ASPNET
{
	public interface IApplicationHost
	{
		object CreateApplicationHost (string virtualDir, string baseDir);
		string Path { get; }	
		string VPath { get; }	
	}
}

