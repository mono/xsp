pkgconfigdir=$(libdir)/pkgconfig
builddir=$(top_builddir)/src/Mono.WebServer
MCSFLAGS= -debug+ -debug:full -nologo -unsafe -nowarn:618 $(WEBTRACING)

monowebserverdir = $(prefix)/lib/xsp/1.0
monowebserver2dir = $(prefix)/lib/xsp/2.0

pkgconfig_DATA = xsp.pc xsp-2.pc
noinst_SCRIPTS= Mono.WebServer.dll $(monowebserver2_install)

GACUTIL1=$(GACUTIL) -package 1.0
if NET_2_0
monowebserver2_install = Mono.WebServer2.dll
monowebserver2_references = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll
GACUTIL2=$(GACUTIL) -package 2.0
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
			XSPWorkerRequest.cs \
			Tracing.cs

monowebserver_build_sources = $(addprefix $(srcdir)/, $(monowebserver_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo.cs)
monowebserver2_build_sources = $(addprefix $(srcdir)/, $(monowebserver_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo2.cs)

Mono.WebServer.dll: $(monowebserver_build_sources)
	$(MCS) $(MCSFLAGS) $(monowebserver_references) /target:library /out:$@ $(monowebserver_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

Mono.WebServer2.dll: $(monowebserver2_build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(monowebserver2_references) /target:library /out:$@ $(monowebserver2_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL1) $(GACUTIL_FLAGS) -i $(builddir)/Mono.WebServer.dll

#if NET_2_0
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/Mono.WebServer2.dll
#endif
	
uninstall-local:
	-for i in Mono.WebServer Mono.WebServer2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

