MCSFLAGS= -debug+ -debug:full -nologo

monowebserverdir = $(prefix)/lib/mono/1.0
monowebserver2dir = $(prefix)/lib/mono/2.0

monowebserver_SCRIPTS = Mono.WebServer.dll
monowebserver2_SCRIPTS = $(monowebserver2_install)

if NET_2_0
monowebserver2_install = Mono.WebServer.dll
endif

CLEANFILES = Mono.WebServer.dll Mono.WebServer.dll.mdb Mono.WebServer.dll.pdb
EXTRA_DIST = $(monowebserver_sources) key.snk

# 
monowebserver_references= -r:System.Web.dll -r:Mono.Security.dll

monowebserver_sources  = \
			AssemblyInfo.cs \
			ApplicationServer.cs \
			BaseApplicationHost.cs \
			BaseRequestBroker.cs \
			IApplicationHost.cs \
			InitialWorkerRequest.cs \
			IWebSource.cs \
			LingeringNetworkStream.cs \
			MonoWorkerRequest.cs \
			XSPApplicationHost.cs \
			XSPWorkerRequest.cs

monowebserver_build_sources = $(addprefix $(srcdir)/, $(monowebserver_sources))

Mono.WebServer.dll: $(monowebserver_build_sources) key.snk
	$(MCS) $(MCSFLAGS) $(monowebserver_references) /target:library /out:$@ /keyfile:$(srcdir)/key.snk $(monowebserver_build_sources)

install-data-local:
	$(GACUTIL) -i Mono.WebServer.dll

uninstall-local:
	$(GACUTIL) -u Mono.WebServer

