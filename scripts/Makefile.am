EXTRA_DIST = script.in

bin2_scripts_real = xsp2 mod-mono-server2 fastcgi-mono-server2
if NET_2_0
bin2_scripts = $(bin2_scripts_real)
tool2_scripts = asp-state2 dbsessmgr2
endif

bin_scripts = xsp mod-mono-server fastcgi-mono-server
tool_scripts = asp-state dbsessmgr

bin1_scripts = xsp1 mod-mono-server1 fastcgi-mono-server1
tool1_scripts = asp-state1 dbsessmgr1

bin_SCRIPTS = $(bin1_scripts) $(bin2_scripts) $(tool1_scripts) $(tool2_scripts) $(tool_scripts) $(bin_scripts)

CLEANFILES = $(bin1_scripts) $(bin2_scripts_real) $(tool1_scripts) $(tool2_scripts) $(bin_scripts) $(tool_scripts)

plat_bindir = $(prefix)/lib/mono/1.0
plat_bindir2 = $(prefix)/lib/mono/2.0
plat_tooldir = $(prefix)/lib/xsp/1.0
plat_tooldir2 = $(prefix)/lib/xsp/2.0

REWRITE = sed \
	-e 's,@''plat_bindir@,$(plat_bindir),g'		\
	-e 's,@''exe_file@,$@.exe,g'			\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITE2 = sed \
	-e 's,@''plat_bindir@,$(plat_bindir2),g'		\
	-e 's,@''exe_file@,$@.exe,g'				\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITE_TOOLS = sed \
	-e 's,@''plat_bindir@,$(plat_tooldir),g'		\
	-e 's,@''exe_file@,$@.exe,g'			\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITE_TOOLS2 = sed \
	-e 's,@''plat_bindir@,$(plat_tooldir2),g'		\
	-e 's,@''exe_file@,$@.exe,g'				\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

$(bin1_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool1_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

if NET_2_0
$(bin2_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool2_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(bin_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@
else
$(bin_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@
endif

