builddir=$(top_builddir)/src/Mono.WebServer.XSP

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING)

XSP2_EXE = xsp2.exe
GACUTIL2=$(GACUTIL) -package 2.0

noinst_SCRIPTS = $(XSP2_EXE)

CLEANFILES = *.exe *.mdb

# 
references2 = -r:System.Web.dll -r:System.Configuration.dll -r:../Mono.WebServer/Mono.WebServer2.dll -r:Mono.Security.dll 

sources = main.cs SecurityConfiguration.cs

build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in

xsp2.exe: $(build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/xsp2.exe

uninstall-local:
	-for i in xsp2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
