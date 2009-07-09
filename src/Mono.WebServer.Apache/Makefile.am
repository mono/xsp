builddir=$(top_builddir)/src/Mono.WebServer.Apache

MCSFLAGS= -unsafe -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING)

GACUTIL1=$(GACUTIL) -package 1.0

if NET_2_0
MODMONOSERVER2_EXE = mod-mono-server2.exe
GACUTIL2=$(GACUTIL) -package 2.0
endif

noinst_SCRIPTS=mod-mono-server.exe $(MODMONOSERVER2_EXE)

CLEANFILES = *.exe *.mdb

if PLATFORM_WIN32
references= -lib:"$(prefix)/lib" -r:../Mono.WebServer/Mono.WebServer.dll \
		-r:System.Web.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
references2= -lib:"$(prefix)/lib" -r:../Mono.WebServer/Mono.WebServer2.dll \
			-r:System.Web.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
else
references= -r:System.Web.dll -r:../Mono.WebServer/Mono.WebServer.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
references2= -r:System.Web.dll -r:System.Configuration.dll -r:../Mono.WebServer/Mono.WebServer2.dll \
			-r:Mono.Posix.dll -r:Mono.Security.dll 
endif

sources = $(shell cat $(srcdir)/Mono.WebServer.Apache.sources)

build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in Mono.WebServer.Apache.sources


mod-mono-server.exe: $(sources) AssemblyInfo.cs
	$(MCS) $(MCSFLAGS) $(references) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

mod-mono-server2.exe: $(sources) AssemblyInfo.cs
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL1) $(GACUTIL_FLAGS) -i $(builddir)/mod-mono-server.exe
#if NET_2_0
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/mod-mono-server2.exe
#endif

uninstall-local:
	-for i in mod-mono-server mod-mono-server2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
