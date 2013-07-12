EXTRA_DIST = script.in

bin2_scripts_real = xsp2 mod-mono-server2 fastcgi-mono-server2
bin2_scripts = $(bin2_scripts_real)
tool2_scripts = asp-state2 dbsessmgr2

if NET_4_0
bin4_scripts = xsp4 mod-mono-server4 fastcgi-mono-server4
tool4_scripts = asp-state4 dbsessmgr4
endif

bin_scripts = xsp mod-mono-server fastcgi-mono-server
tool_scripts = asp-state dbsessmgr

binplain_scripts = mono-fpm

bin_SCRIPTS = $(bin2_scripts) $(tool2_scripts) $(tool_scripts) $(bin_scripts) $(bin4_scripts) $(tool4_scripts) $(binplain_scripts)

CLEANFILES = $(bin2_scripts_real) $(tool2_scripts) $(bin_scripts) $(tool_scripts) $(bin4_scripts) $(tool4_scripts) $(binplain_scripts)

plat_bindir2 = $(prefix)/lib/mono/2.0
plat_tooldir2 = $(prefix)/lib/xsp/2.0

if NET_4_0
plat_bindir4 = $(prefix)/lib/mono/4.5
plat_tooldir4 = $(prefix)/lib/xsp/4.0
endif

REWRITE2 = sed \
	-e 's,@''plat_bindir@,$(plat_bindir2),g'		\
	-e 's,@''exe_file@,'`basename $@ 2`'2.exe,g'		\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITE_TOOLS2 = echo dd $@ dd; sed \
	-e 's,@''plat_bindir@,$(plat_tooldir2),g'		\
	-e 's,@''exe_file@,'`basename $@ 2`'2.exe,g'		\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITEPLAIN = sed \
	-e 's,@''plat_bindir@,$(plat_bindir4),g'		\
	-e 's,@''exe_file@,'`basename $@ 4`'.exe,g'		\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

if NET_4_0
REWRITE4 = sed \
	-e 's,@''plat_bindir@,$(plat_bindir4),g'		\
	-e 's,@''exe_file@,'`basename $@ 4`'4.exe,g'		\
	-e 's,@''RUNTIME@,@RUNTIME@,g'

REWRITE_TOOLS4 = echo dd $@ dd; sed \
	-e 's,@''plat_bindir@,$(plat_tooldir4),g'		\
	-e 's,@''exe_file@,'`basename $@ 4`'4.exe,g'		\
	-e 's,@''RUNTIME@,@RUNTIME@,g'
endif

$(bin2_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool2_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(bin_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(binplain_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITEPLAIN) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

if NET_4_0
$(bin4_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE4) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

$(tool4_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE_TOOLS4) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@
endif
