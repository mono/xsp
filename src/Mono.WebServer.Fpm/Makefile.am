builddir=$(top_builddir)/src/Mono.WebServer.Fpm

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -unsafe+ -keyfile:$(top_srcdir)/src/mono.snk

XSP_EXE = mono-fpm.exe
GACUTIL4=$(GACUTIL) -package 4.5

noinst_SCRIPTS = $(XSP_EXE) 

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll -r:Mono.Posix.dll
references = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll -r:../Mono.WebServer.FastCgi/fastcgi-mono-server4.exe

sources = $(shell cat $(srcdir)/Mono.WebServer.Fpm.sources)
build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in $(resources) Mono.WebServer.Fpm.sources

mono-fpm.exe: $(build_sources)
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references) /out:$@ \
		$(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/mono-fpm.exe

uninstall-local:
	-for i in $(noinst_SCRIPTS) ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
