pkgconfigdir=$(libdir)/pkgconfig
builddir=$(top_builddir)/src/Mono.WebServer
MCSFLAGS= -debug+ -debug:full -nologo -unsafe -nowarn:618 $(WEBTRACING) -doc:$(top_builddir)/docs/Mono.WebServer.xml
SYSTEMCORE_DEP=@SYSTEMCORE_DEP@

monowebserver2dir = $(prefix)/lib/xsp/2.0

pkgconfig_DATA = xsp-2.pc
noinst_SCRIPTS= $(monowebserver2_install)

monowebserver2_install = Mono.WebServer2.dll
monowebserver2_references = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll 

GACUTIL2=$(GACUTIL) -package 2.0

CLEANFILES = Mono.WebServer.dll* Mono.WebServer2.dll*
EXTRA_DIST = $(monowebserver_sources) AssemblyInfo.cs.in AssemblyInfo2.cs.in

monowebserver2_sources  = \
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
			XSPRequestBroker.cs \
			XSPWebSource.cs \
			XSPWorker.cs \
			HttpErrors.cs \
			Paths.cs \
			VPathToHost.cs \
			Tracing.cs \
			SearchPattern.cs \
			ReaderWriterLockSlim.cs \
			LockRecursionException.cs \
			LockRecursionPolicy.cs

monowebserver2_build_sources = $(addprefix $(srcdir)/, $(monowebserver2_sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo2.cs)

Mono.WebServer2.dll: $(monowebserver2_build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(monowebserver2_references) /target:library /out:$@ $(monowebserver2_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/Mono.WebServer2.dll

uninstall-local:
	-for i in Mono.WebServer2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

