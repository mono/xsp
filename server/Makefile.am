MCSFLAGS= -debug+ -debug:full -nologo

xspdir = $(bindir)
modmonoserverdir = $(bindir)

xsp_SCRIPTS = xsp.exe
modmonoserver_SCRIPTS = mod-mono-server.exe

CLEANFILES = xsp.exe mod-mono-server.exe
EXTRA_DIST = $(common) $(xsp_only) $(modmono_only)

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

xsp_only = AssemblyInfo.cs.in \
	  InitialWorkerRequest.cs \
	  XSPApplicationHost.cs \
	  XSPWorkerRequest.cs

xsp_sources = $(common) $(xsp_only:.in=)
xsp_build_sources = $(addprefix $(srcdir)/, $(xsp_sources))

modmono_only = ModMonoRequest.cs \
	      ModMonoWorkerRequest.cs \
	      ModMonoApplicationHost.cs \
	      ModMonoRequest.cs \
	      ModMonoTCPWebSource.cs \
	      AssemblyInfoModMono.cs.in

modmono_sources =  $(common) $(modmono_only:.in=)
modmono_build_sources = $(addprefix $(srcdir)/, $(modmono_sources))

xsp.exe: $(xsp_build_sources)
	$(MCS) $(MCSFLAGS) $(xsp_references) /out:$@ $^

mod-mono-server.exe: $(modmono_build_sources)
	$(MCS) $(MCSFLAGS) $(modmono_references) /d:MODMONO_SERVER /out:$@ $^

