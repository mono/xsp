pkgconfigdir=$(libdir)/pkgconfig
builddir=$(top_builddir)/src/Mono.WebServer
MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -unsafe -keyfile:$(top_srcdir)/src/mono.snk
SYSTEMCORE_DEP=@SYSTEMCORE_DEP@

monowebserver4dir = $(prefix)/lib/xsp/4.5
monowebserver4_install = 4.0/Mono.WebServer2.dll
monowebserver4_references = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll -r:System.Core.dll -r:Mono.Posix.dll
GACUTIL4=$(GACUTIL) -package 4.5

noinst_SCRIPTS= $(monowebserver4_install)

CLEANFILES = 4.0/Mono.WebServer2.dll*
EXTRA_DIST = AssemblyInfo4.cs.in

sources = $(shell cat $(srcdir)/Mono.WebServer.sources)

monowebserver4_build_sources = $(addprefix $(srcdir)/, $(sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo4.cs)

4.0/Mono.WebServer2.dll: $(monowebserver4_build_sources)
	-$(MKDIR_P) 4.0
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(monowebserver4_references) /target:library /out:$@ $(monowebserver4_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/4.0/Mono.WebServer2.dll

uninstall-local:
	-for i in Mono.WebServer2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

clean-local:
	rm -rf 4.0
