builddir=$(top_builddir)/src/Mono.WebServer.XSP

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING)

GACUTIL1=$(GACUTIL) -package 1.0

if NET_2_0
XSP2_EXE = xsp2.exe
GACUTIL2=$(GACUTIL) -package 2.0
endif

noinst_SCRIPTS=xsp.exe xsp.exe $(XSP2_EXE)

CLEANFILES = *.exe *.mdb

# 
references = -r:System.Web.dll -r:../Mono.WebServer/Mono.WebServer.dll -r:Mono.Security.dll 
references2 = -r:System.Web.dll -r:System.Configuration.dll -r:../Mono.WebServer/Mono.WebServer2.dll -r:Mono.Security.dll 

sources = main.cs SecurityConfiguration.cs

build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

EXTRA_DIST = $(sources) AssemblyInfo.cs.in

xsp.exe: $(build_sources)
	$(MCS) $(MCSFLAGS) $(references) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(builddir)/../mono.snk

xsp2.exe: $(build_sources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(builddir)/../mono.snk

install-data-local:
	$(GACUTIL1) $(GACUTIL_FLAGS) -i $(builddir)/xsp.exe

#if NET_2_0
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/xsp2.exe
#endif

uninstall-local:
	-for i in xsp xsp2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $i ; \
	done
