builddir=$(top_builddir)/src/Mono.WebServer.Apache

MCSFLAGS= -unsafe -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -keyfile:$(top_srcdir)/src/mono.snk

MODMONOSERVER2_EXE = mod-mono-server2.exe
GACUTIL2=$(GACUTIL) -package 2.0

if NET_4_0
MODMONOSERVER4_EXE = mod-mono-server4.exe
GACUTIL4=$(GACUTIL) -package 4.5
endif

noinst_SCRIPTS = $(MODMONOSERVER2_EXE) $(MODMONOSERVER4_EXE)

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Posix.dll -r:Mono.Security.dll 
references2 = $(references_common) -r:../Mono.WebServer/2.0/Mono.WebServer2.dll

if NET_4_0
references4 = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll
endif

if PLATFORM_WIN32
references2 += -lib:"$(prefix)/lib"
if NET_4_0
references4 += -lib:"$(prefix)/lib"
endif
endif

sources = $(shell cat $(srcdir)/Mono.WebServer.Apache.sources)

build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in Mono.WebServer.Apache.sources

mod-mono-server2.exe: $(sources) AssemblyInfo.cs
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

if NET_4_0
mod-mono-server4.exe: $(sources) AssemblyInfo.cs
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references4) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/mod-mono-server2.exe
if NET_4_0
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/mod-mono-server4.exe
endif

uninstall-local:
	-for i in $(noinst_SCRIPTS); do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
