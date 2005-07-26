pkgconfigdir=$(libdir)/pkgconfig
MCSFLAGS= -debug+ -debug:full -nologo

monowebserverdir = $(prefix)/lib/xsp/1.0
monowebserver2dir = $(prefix)/lib/xsp/2.0

pkgconfig_DATA = xsp.pc xsp-2.pc
monowebserver_SCRIPTS = Mono.WebServer.dll
monowebserver2_SCRIPTS = $(monowebserver2_install)

if NET_2_0
monowebserver2_install = Mono.WebServer2.dll
endif

CLEANFILES = Mono.WebServer.dll* Mono.WebServer2.dll*
EXTRA_DIST = $(monowebserver_sources) AssemblyInfo.cs.in AssemblyInfo2.cs.in

# 
monowebserver_references= -r:System.Web.dll -r:Mono.Security.dll

monowebserver_sources  = \
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

monowebserver_build_sources = $(addprefix $(srcdir)/, $(monowebserver_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo.cs)
monowebserver2_build_sources = $(addprefix $(srcdir)/, $(monowebserver_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo2.cs)

Mono.WebServer.dll: $(monowebserver_build_sources)
	$(MCS) $(MCSFLAGS) $(monowebserver_references) /target:library /out:$@ $(monowebserver_build_sources)

Mono.WebServer2.dll: $(monowebserver2_build_sources)
	$(GMCS) $(MCSFLAGS) $(monowebserver_references) /target:library /out:$@ $(monowebserver2_build_sources)

