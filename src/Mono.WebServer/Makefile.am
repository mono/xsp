pkgconfigdir=$(libdir)/pkgconfig
builddir=$(top_builddir)/src/Mono.WebServer
MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -doc:$(top_builddir)/docs/Mono.WebServer.xml
SYSTEMCORE_DEP=@SYSTEMCORE_DEP@

monowebserver2dir = $(prefix)/lib/xsp/2.0
monowebserver2_install = 2.0/Mono.WebServer2.dll
monowebserver2_references = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll 
GACUTIL2=$(GACUTIL) -package 2.0

if NET_4_0
monowebserver4dir = $(prefix)/lib/xsp/4.0
monowebserver4_install = 4.0/Mono.WebServer2.dll
monowebserver4_references = $(monowebserver2_references)
GACUTIL4=$(GACUTIL) -package 4.0
endif

noinst_SCRIPTS= $(monowebserver2_install) $(monowebserver4_install)

CLEANFILES = 2.0/Mono.WebServer2.dll* 4.0/Mono.WebServer2.dll*
EXTRA_DIST = $(monowebserver2_sources) AssemblyInfo.cs.in AssemblyInfo2.cs.in AssemblyInfo4.cs.in

monowebserver2_sources  = \
			ApplicationServer.cs \
			BaseApplicationHost.cs \
			BaseRequestBroker.cs \
			IApplicationHost.cs \
			InitialWorkerRequest.cs \
			WebSource.cs \
			Worker.cs \
			LingeringNetworkStream.cs \
			MonoWorkerRequest.cs \
			HttpErrors.cs \
			Paths.cs \
			VPathToHost.cs \
			WebTrace.cs \
			SearchPattern.cs \
			ReaderWriterLockSlim.cs \
			LockRecursionException.cs \
			LockRecursionPolicy.cs \
			UnregisterRequestEventArgs.cs \
			IRequestBroker.cs \
			RequestData.cs \
			RequestLineException.cs \
			MapPathEventArgs.cs \
			MapPathEventHandler.cs \
			EndOfRequestHandler.cs

monowebserver2_build_sources = $(addprefix $(srcdir)/, $(monowebserver2_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo2.cs)

if NET_4_0
monowebserver4_build_sources = $(addprefix $(srcdir)/, $(monowebserver2_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo4.cs)
endif

2.0/Mono.WebServer2.dll: $(monowebserver2_build_sources)
	-mkdir 2.0
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(monowebserver2_references) /target:library /out:$@ $(monowebserver2_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

if NET_4_0
4.0/Mono.WebServer2.dll: $(monowebserver4_build_sources)
	-mkdir 4.0
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(monowebserver4_references) /target:library /out:$@ $(monowebserver4_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/2.0/Mono.WebServer2.dll
if NET_4_0
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/4.0/Mono.WebServer2.dll
endif

uninstall-local:
	-for i in Mono.WebServer2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

clean-local:
	rm -rf 2.0 4.0
