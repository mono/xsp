SUBDIRS=Mono.WebServer
builddir=$(top_builddir)/src

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING)

xspdir = $(prefix)/lib/xsp/1.0
modmonoserverdir = $(prefix)/lib/xsp/1.0
xsp2dir = $(prefix)/lib/xsp/2.0
modmonoserver2dir = $(prefix)/lib/xsp/2.0

GACUTIL1=$(GACUTIL) -package 1.0
if NET_2_0
XSP2_EXE = xsp2.exe
MODMONOSERVER2_EXE = mod-mono-server2.exe
GACUTIL2=$(GACUTIL) -package 2.0
endif

if !XSP_ONLY
modmonoserver_SCR = mod-mono-server.exe
modmonoserver2_SCR = $(MODMONOSERVER2_EXE)
endif

noinst_SCRIPTS=xsp.exe xsp.exe $(XSP2_EXE) $(modmonoserver_SCR) $(modmonoserver2_SCR)

CLEANFILES = *.exe *.mdb

# 
xsp_references= -r:System.Web.dll -r:Mono.WebServer/Mono.WebServer.dll -r:Mono.Security.dll 
xsp2_references= -r:System.Web.dll -r:System.Configuration.dll -r:Mono.WebServer/Mono.WebServer2.dll -r:Mono.Security.dll 
if PLATFORM_WIN32
modmono_references= -lib:"$(prefix)/lib" -r:Mono.WebServer/Mono.WebServer.dll \
			-r:System.Web.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
modmono2_references= -lib:"$(prefix)/lib" -r:Mono.WebServer/Mono.WebServer2.dll \
			-r:System.Web.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
else
modmono_references= -r:System.Web.dll -r:Mono.WebServer/Mono.WebServer.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
modmono2_references= -r:System.Web.dll -r:System.Configuration.dll -r:Mono.WebServer/Mono.WebServer2.dll \
			-r:Mono.Posix.dll -r:Mono.Security.dll 
endif

xsp_sources = server.cs security.cs

xsp_build_sources = $(addprefix $(srcdir)/, $(xsp_sources)) AssemblyInfo.cs

modmono_only = ModMonoRequest.cs \
	      ModMonoWorkerRequest.cs \
	      ModMonoApplicationHost.cs \
	      ModMonoTCPWebSource.cs \
	      ModMonoRequestBroker.cs \
	      ModMonoWebSource.cs \
	      ModMonoWorker.cs

modmono_sources =  $(modmono_only) $(xsp_sources)
modmono_build_sources = $(addprefix $(srcdir)/, $(modmono_sources)) AssemblyInfoModMono.cs
EXTRA_DIST = $(xsp_sources) $(modmono_only) AssemblyInfo.cs.in AssemblyInfoModMono.cs.in mono.pub mono.snk


xsp.exe: $(xsp_build_sources)
	$(MCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/mono.snk

mod-mono-server.exe: $(modmono_build_sources)
	$(MCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/mono.snk

xsp2.exe: $(xsp_build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(xsp2_references) /out:$@ $(xsp_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/mono.snk

mod-mono-server2.exe: $(modmono_build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(modmono2_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/mono.snk

install-data-local:
	for i in xsp.exe $(modmonoserver_SCR) ; do \
		$(GACUTIL1) $(GACUTIL_FLAGS) -i $(top_builddir)/src/$$i ; \
	done

#if NET_2_0
	for i in xsp2.exe $(modmonoserver2_SCR) ; do \
		$(GACUTIL2) $(GACUTIL_FLAGS) -i $(top_builddir)/src/$$i ; \
	done
#endif
	
uninstall-local:
	-for i in xsp mod-mono-server xsp2 mod-mono-server2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

