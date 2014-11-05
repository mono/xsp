builddir=$(top_builddir)/src/Mono.WebServer.Apache

MCSFLAGS= -unsafe -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -keyfile:$(top_srcdir)/src/mono.snk

MODMONOSERVER4_EXE = mod-mono-server4.exe
GACUTIL4=$(GACUTIL) -package 4.5

noinst_SCRIPTS = $(MODMONOSERVER4_EXE)

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Posix.dll -r:Mono.Security.dll 

references4 = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll

if PLATFORM_WIN32
references4 += -lib:"$(prefix)/lib"
endif

sources = $(shell cat $(srcdir)/Mono.WebServer.Apache.sources)

build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in Mono.WebServer.Apache.sources

mod-mono-server4.exe: $(sources) AssemblyInfo.cs
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references4) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/mod-mono-server4.exe

uninstall-local:
	-for i in $(noinst_SCRIPTS); do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
