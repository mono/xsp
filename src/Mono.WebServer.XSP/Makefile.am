builddir=$(top_builddir)/src/Mono.WebServer.XSP
pkgconfigdir=$(libdir)/pkgconfig

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 -unsafe $(WEBTRACING) -keyfile:$(top_srcdir)/src/mono.snk

XSP4_EXE = xsp4.exe
GACUTIL4=$(GACUTIL) -package 4.5

noinst_SCRIPTS = $(XSP4_EXE)

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll 

references4 = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll

pkgconfig_DATA = xsp-2.pc
pkgconfig_DATA += xsp-4.pc

sources = $(shell cat $(srcdir)/Mono.WebServer.XSP.sources)
build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in

xsp4.exe: $(build_sources)
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references4) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/xsp4.exe

uninstall-local:
	-for i in xsp4 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
