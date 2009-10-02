EXTRA_DIST = script.in

bin2_scripts_real = xsp2 mod-mono-server2 fastcgi-mono-server2
bin2_scripts = $(bin2_scripts_real)
tool2_scripts = asp-state2 dbsessmgr2

bin_scripts = xsp mod-mono-server fastcgi-mono-server
tool_scripts = asp-state dbsessmgr

bin_SCRIPTS = $(bin2_scripts) $(tool2_scripts) $(tool_scripts) $(bin_scripts)

CLEANFILES = $(bin2_scripts_real) $(tool2_scripts) $(bin_scripts) $(tool_scripts)

plat_bindir2 = $(prefix)/lib/mono/2.0
plat_tooldir2 = $(prefix)/lib/xsp/2.0

REWRITE2 = sed \
	-e 's,@''plat_bindir@,$(plat_bindir2),g'		\
	-e 's,@''exe_file@,'`basename $@ 2`'2.exe,g'				\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITE_TOOLS2 = echo dd $@ dd; sed \
	-e 's,@''plat_bindir@,$(plat_tooldir2),g'		\
	-e 's,@''exe_file@,'`basename $@ 2`'2.exe,g'		\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

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
