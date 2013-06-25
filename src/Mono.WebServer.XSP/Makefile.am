builddir=$(top_builddir)/src/Mono.WebServer.XSP
pkgconfigdir=$(libdir)/pkgconfig

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 -unsafe $(WEBTRACING) -keyfile:$(top_srcdir)/src/mono.snk

XSP2_EXE = xsp2.exe
GACUTIL2=$(GACUTIL) -package 2.0

if NET_4_0
XSP4_EXE = xsp4.exe
GACUTIL4=$(GACUTIL) -package 4.5
endif

noinst_SCRIPTS = $(XSP2_EXE) $(XSP4_EXE)

CLEANFILES = *.exe *.mdb

references_common = -r:System.Web.dll -r:System.Configuration.dll -r:Mono.Security.dll 
references2 = $(references_common) -r:../Mono.WebServer/2.0/Mono.WebServer2.dll

if NET_4_0
references4 = $(references_common) -r:../Mono.WebServer/4.0/Mono.WebServer2.dll
endif

pkgconfig_DATA = xsp-2.pc
if NET_4_0
pkgconfig_DATA += xsp-4.pc
endif

sources = $(shell cat $(srcdir)/Mono.WebServer.XSP.sources)
build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in

xsp2.exe: $(build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

if NET_4_0
xsp4.exe: $(build_sources)
	$(DMCS) -d:NET_2_0 -d:NET_4_0 $(MCSFLAGS) $(references4) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/xsp2.exe
if NET_4_0
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(builddir)/xsp4.exe
endif

uninstall-local:
	-for i in xsp2 xsp4 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
