MCSFLAGS= -debug+ -debug:full -nologo

monowebserverdir = $(prefix)/lib/mono/1.0

monowebserver_SCRIPTS = Mono.WebServer.dll

CLEANFILES = Mono.WebServer.dll Mono.WebServer.dll.mdb Mono.WebServer.dll.pdb

EXTRA_DIST = $(monowebserver_sources)

# 
monowebserver_references= -r:System.Web.dll -r:Mono.Security.dll

monowebserver_sources  = IApplicationHost.cs \
			MonoWorkerRequest.cs \
			ApplicationServer.cs \
			LingeringNetworkStream.cs \
			BaseApplicationHost.cs \
			BaseRequestBroker.cs \
			IWebSource.cs \
			AssemblyInfo.cs

monowebserver_build_sources = $(addprefix $(srcdir)/, $(monowebserver_sources))

Mono.WebServer.dll: $(monowebserver_build_sources)
	$(MCS) $(MCSFLAGS) $(monowebserver_references) /target:library /out:$@ $(monowebserver_build_sources)

