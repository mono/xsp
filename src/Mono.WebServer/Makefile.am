pkgconfigdir=$(libdir)/pkgconfig
builddir=$(top_builddir)/src/Mono.WebServer
MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -unsafe -keyfile:$(top_srcdir)/src/mono.snk
SYSTEMCORE_DEP=@SYSTEMCORE_DEP@

monowebserver2dir = $(prefix)/lib/xsp/2.0
monowebserver2_install = 2.0/Mono.WebServer2.dll
monowebserver2_references = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll -r:System.Core.dll -r:Mono.Posix.dll
GACUTIL2=$(GACUTIL) -package 2.0

if NET_4_0
monowebserver4dir = $(prefix)/lib/xsp/4.5
monowebserver4_install = 4.0/Mono.WebServer2.dll
monowebserver4_references = $(monowebserver2_references)
GACUTIL4=$(GACUTIL) -package 4.5
endif

noinst_SCRIPTS= $(monowebserver2_install) $(monowebserver4_install)

CLEANFILES = 2.0/Mono.WebServer2.dll* 4.0/Mono.WebServer2.dll*
EXTRA_DIST = $(monowebserver2_sources) AssemblyInfo.cs.in AssemblyInfo2.cs.in AssemblyInfo4.cs.in

sources = $(shell cat $(srcdir)/Mono.WebServer.sources)

monowebserver2_build_sources = $(addprefix $(srcdir)/, $(sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo2.cs)

if NET_4_0
monowebserver4_build_sources = $(addprefix $(srcdir)/, $(sources)) $(addprefix $(top_builddir)/src/Mono.WebServer/, AssemblyInfo4.cs)
endif

2.0/Mono.WebServer2.dll: $(monowebserver2_build_sources)
	-$(MKDIR_P) 2.0
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(monowebserver2_references) /target:library /out:$@ $(monowebserver2_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

if NET_4_0
4.0/Mono.WebServer2.dll: $(monowebserver4_build_sources)
	-$(MKDIR_P) 4.0
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(monowebserver4_references) /target:library /out:$@ $(monowebserver4_build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/2.0/Mono.WebServer2.dll
if NET_4_0
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/4.0/Mono.WebServer2.dll
endif

uninstall-local:
	-for i in Mono.WebServer2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

clean-local:
	rm -rf 2.0 4.0
