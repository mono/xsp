builddir=$(top_builddir)/src/Mono.WebServer.FastCgi

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -unsafe+ -keyfile:$(top_srcdir)/src/mono.snk

XSP4_EXE = fastcgi-mono-server4.exe
GACUTIL4=$(GACUTIL) -package 4.5

noinst_SCRIPTS = $(XSP4_EXE)

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll -r:Mono.Posix.dll

references4 = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll

sources = $(shell cat $(srcdir)/Mono.WebServer.FastCgi.sources)
build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in $(resources) Mono.WebServer.FastCgi.sources

fastcgi-mono-server4.exe: $(build_sources)
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references4) /out:$@ \
		$(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/fastcgi-mono-server4.exe

uninstall-local:
	-for i in $(noinst_SCRIPTS) ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
