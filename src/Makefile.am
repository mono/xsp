SUBDIRS=Mono.WebServer

MCSFLAGS= -debug+ -debug:full -nologo

xspdir = $(prefix)/lib/mono/1.0
modmonoserverdir = $(prefix)/lib/mono/1.0
xsp2dir = $(prefix)/lib/mono/2.0
modmonoserver2dir = $(prefix)/lib/mono/2.0

if NET_2_0
XSP2_EXE = xsp2.exe
MODMONOSERVER2_EXE = mod-mono-server2.exe
endif

xsp_SCRIPTS = xsp.exe
xsp2_SCRIPTS = $(XSP2_EXE)

if !XSP_ONLY
modmonoserver_SCRIPTS = mod-mono-server.exe
modmonoserver2_SCRIPTS = $(MODMONOSERVER2_EXE)
endif

CLEANFILES = *.exe *.mdb

EXTRA_DIST = $(xsp_sources) $(modmono_only) AssemblyInfo.cs.in AssemblyInfoModMono.cs.in

# 
xsp_references= -r:System.Web.dll -r:Mono.WebServer/Mono.WebServer.dll -r:Mono.Security.dll 
if PLATFORM_WIN32
modmono_references= -lib:"$(prefix)/lib" -r:Mono.WebServer/Mono.WebServer.dll \
			-r:System.Web.dll -r:Mono.Posix.dll
else
modmono_references= -r:System.Web.dll -r:Mono.WebServer/Mono.WebServer.dll -r:Mono.Posix.dll
endif

xsp_sources = server.cs

xsp_build_sources = $(addprefix $(srcdir)/, $(xsp_sources)) AssemblyInfo.cs

modmono_only = ModMonoRequest.cs \
	      ModMonoWorkerRequest.cs \
	      ModMonoApplicationHost.cs \
	      ModMonoTCPWebSource.cs

modmono_sources =  $(modmono_only) server.cs
modmono_build_sources = $(addprefix $(srcdir)/, $(modmono_sources)) AssemblyInfoModMono.cs

xsp.exe: $(xsp_build_sources)
	$(MCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)

mod-mono-server.exe: $(modmono_build_sources)
	$(MCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)

xsp2.exe: $(xsp_build_sources)
	$(GMCS) $(MCSFLAGS) $(xsp_references) /out:$@ $(xsp_build_sources)

mod-mono-server2.exe: $(modmono_build_sources)
	$(GMCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $(modmono_build_sources)

