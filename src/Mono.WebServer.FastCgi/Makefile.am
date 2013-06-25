builddir=$(top_builddir)/src/Mono.WebServer.FastCgi

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -unsafe+ -keyfile:$(top_srcdir)/src/mono.snk

XSP2_EXE = fastcgi-mono-server2.exe
GACUTIL2=$(GACUTIL) -package 2.0

if NET_4_0
XSP4_EXE = fastcgi-mono-server4.exe
GACUTIL4=$(GACUTIL) -package 4.5
endif

noinst_SCRIPTS = $(XSP2_EXE) $(XSP4_EXE)

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll -r:Mono.Posix.dll
references2 = $(references_common) -r:../Mono.WebServer/2.0/Mono.WebServer2.dll

if NET_4_0
references4 = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll
endif

sources = $(shell cat $(srcdir)/Mono.WebServer.FastCgi.sources)
build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in $(resources) Mono.WebServer.FastCgi.sources

fastcgi-mono-server2.exe: $(build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ \
		$(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

if NET_4_0
fastcgi-mono-server4.exe: $(build_sources)
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references4) /out:$@ \
		$(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/fastcgi-mono-server2.exe
if NET_4_0
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/fastcgi-mono-server4.exe
endif

uninstall-local:
	-for i in $(noinst_SCRIPTS) ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
