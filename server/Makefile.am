MCSFLAGS= -debug+ -debug:full -nologo

xspdir = $(prefix)/lib/mono/1.0
modmonoserverdir = $(prefix)/lib/mono/1.0
monoaspnetdir = $(prefix)/lib/mono/1.0
xsp2dir = $(prefix)/lib/mono/2.0
modmonoserver2dir = $(prefix)/lib/mono/2.0

if NET_2_0
XSP2_EXE = xsp2.exe
MODMONOSERVER2_EXE = mod-mono-server2.exe
endif

xsp_SCRIPTS = xsp.exe
xsp2_SCRIPTS = $(XSP2_EXE)
monoaspnet_SCRIPTS = Mono.ASPNET.dll

if !XSP_ONLY
modmonoserver_SCRIPTS = mod-mono-server.exe
modmonoserver2_SCRIPTS = $(MODMONOSERVER2_EXE)
endif

CLEANFILES = *.exe *.mdb

EXTRA_DIST = $(common) $(xsp_only) $(modmono_only) AssemblyInfo.cs.in AssemblyInfoModMono.cs.in

# 
monoaspnet_references= -r:System.Web.dll -r:Mono.Security.dll
xsp_references= -r:System.Web.dll -r:Mono.ASPNET.dll -r:Mono.Security.dll 
if PLATFORM_WIN32
modmono_references= -lib:"$(prefix)/lib" -r:System.Web.dll -r:Mono.Posix.dll
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
	  IWebSource.cs 

xsp_only = \
	  InitialWorkerRequest.cs \
	  XSPApplicationHost.cs \
	  XSPWorkerRequest.cs

xspexe_only = server.cs

xsp_sources = $(xspexe_only)
monoaspnet_sources = $(common) $(xsp_only)
xsp_build_sources = $(addprefix $(srcdir)/, $(xsp_sources)) AssemblyInfo.cs
monoaspnet_build_sources = $(addprefix $(srcdir)/, $(monoaspnet_sources)) AssemblyInfo.cs

modmono_only = ModMonoRequest.cs \
	      ModMonoWorkerRequest.cs \
	      ModMonoApplicationHost.cs \
	      ModMonoTCPWebSource.cs

modmono_sources =  $(common) $(modmono_only) $(xspexe_only)
modmono_build_sources = $(addprefix $(srcdir)/, $(modmono_sources)) AssemblyInfoModMono.cs

Mono.ASPNET.dll: $(monoaspnet_build_sources)
	$(MCS) $(MCSFLAGS) $(monoaspnet_references) /target:library /out:$@ $(monoaspnet_build_sources)

xsp.exe: $(xsp_build_sources)
	$(MCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)

mod-mono-server.exe: $(modmono_build_sources)
	$(MCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)

xsp2.exe: $(xsp_build_sources)
	$(GMCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)

mod-mono-server2.exe: $(modmono_build_sources)
	$(GMCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)

