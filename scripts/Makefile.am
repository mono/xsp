EXTRA_DIST = script.in

if NET_2_0
bin2_scripts = xsp2 mod-mono-server2 asp-state2 dbsessmgr2
endif

bin1_scripts = xsp mod-mono-server asp-state dbsessmgr
bin_SCRIPTS = $(bin1_scripts) $(bin2_scripts)

CLEANFILES = $(bin1_SCRIPTS) $(bin2_SCRIPTS)

plat_bindir = $(libdir)/mono/1.0
plat_bindir2 = $(libdir)/mono/2.0

REWRITE = sed \
	-e 's,@''plat_bindir@,$(plat_bindir),g'		\
	-e 's,@''exe_file@,$@.exe,g'

REWRITE2 = sed \
	-e 's,@''plat_bindir@,$(plat_bindir2),g'		\
	-e 's,@''exe_file@,$@.exe,g'

$(bin1_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@

if NET_2_0
$(bin2_scripts): $(srcdir)/script.in Makefile.am
	$(REWRITE2) $(srcdir)/script.in > $@.tmp
	mv $@.tmp $@
endif
