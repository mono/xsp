builddir=$(top_builddir)/src/Mono.WebServer.FastCgi

MCSFLAGS= -debug+ -debug:full -nologo -nowarn:618 $(WEBTRACING) -unsafe+

GACUTIL1=$(GACUTIL) -package 1.0

if NET_2_0
XSP2_EXE = fastcgi-mono-server2.exe
GACUTIL2=$(GACUTIL) -package 2.0
endif

noinst_SCRIPTS = fastcgi-mono-server.exe $(XSP2_EXE)

CLEANFILES = *.exe *.mdb

# 
references = -r:System.Web.dll -r:../Mono.WebServer/Mono.WebServer.dll -r:Mono.Security.dll -r:Mono.Posix.dll
references2 = -r:System.Web.dll -r:System.Configuration.dll -r:../Mono.WebServer/Mono.WebServer2.dll -r:Mono.Security.dll -r:Mono.Posix.dll

sources = $(shell cat $(srcdir)/Mono.WebServer.FastCgi.sources)
build_sources = $(addprefix $(srcdir)/, $(sources)) AssemblyInfo.cs

resources = ConfigurationManager.xml
build_resources = $(addprefix $(srcdir)/, $(resources))

EXTRA_DIST = $(sources) AssemblyInfo.cs.in $(resources) Mono.WebServer.FastCgi.sources

fastcgi-mono-server.exe: $(build_sources) $(build_resources)
	$(MCS) $(MCSFLAGS) $(references) /out:$@ \
		$(build_resources:%=-resource:%) $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

fastcgi-mono-server2.exe: $(build_sources) $(build_resources)
	$(GMCS) -d:NET_2_0 $(MCSFLAGS) $(references2) /out:$@ \
		$(build_resources:%=-resource:%) $(build_sources)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk

install-data-local:
	$(GACUTIL1) $(GACUTIL_FLAGS) -i $(builddir)/fastcgi-mono-server.exe

#if NET_2_0
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(builddir)/fastcgi-mono-server2.exe
#endif

uninstall-local:
	-for i in fastcgi-mono-server fastcgi-mono-server2 ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done
