MCSFLAGS= -debug+ -debug:full -nologo

xspdir = $(libdir)/mono/1.0
modmonoserverdir = $(libdir)/mono/1.0
xsp2dir = $(libdir)/mono/2.0
modmonoserver2dir = $(libdir)/mono/2.0

if NET_2_0
XSP2_EXE = xsp2.exe
MODMONOSERVER2_EXE = mod-mono-server2.exe
endif

xsp_SCRIPTS = xsp.exe
xsp2_SCRIPTS = $(XSP2_EXE)
modmonoserver_SCRIPTS = mod-mono-server.exe $(MODMONOSERVER2_EXE)
modmonoserver2_SCRIPTS = $(MODMONOSERVER2_EXE)

CLEANFILES = *.exe *.mdb

EXTRA_DIST = $(common) $(xsp_only) $(modmono_only) AssemblyInfo.cs.in AssemblyInfoModMono.cs.in

# 
xsp_references= -r:System.Web.dll
if PLATFORM_WIN32
modmono_references= -lib:"$(libdir)" -r:System.Web.dll -r:Mono.Posix.dll
else
modmono_references= -r:System.Web.dll -r:Mono.Posix.dll
endif

common  = IApplicationHost.cs \
	  MonoWorkerRequest.cs \
	  Tracing.cs \
	  ApplicationServer.cs \
	  LingeringNetworkStream.cs \
	  BaseApplicationHost.cs \
	  BaseRequestBroker.cs \
	  IWebSource.cs \
	  server.cs

xsp_only = \
	  InitialWorkerRequest.cs \
	  XSPApplicationHost.cs \
	  XSPWorkerRequest.cs

xsp_sources = $(common) $(xsp_only)
xsp_build_sources = $(addprefix $(srcdir)/, $(xsp_sources)) AssemblyInfo.cs

modmono_only = ModMonoRequest.cs \
	      ModMonoWorkerRequest.cs \
	      ModMonoApplicationHost.cs \
	      ModMonoTCPWebSource.cs

modmono_sources =  $(common) $(modmono_only)
modmono_build_sources = $(addprefix $(srcdir)/, $(modmono_sources)) AssemblyInfoModMono.cs

xsp.exe: $(xsp_build_sources)
	$(MCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)

mod-mono-server.exe: $(modmono_build_sources)
	$(MCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)

xsp2.exe: $(xsp_build_sources)
	$(GMCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)

mod-mono-server2.exe: $(modmono_build_sources)
	$(GMCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)

