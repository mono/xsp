EXTRA_DIST = script.in

bin_SCRIPTS = xsp mod-mono-server

CLEANFILES = $(bin_SCRIPTS)

plat_bindir = $(libdir)/mono/1.0

REWRITE = sed \
	-e 's,@''plat_bindir@,$(plat_bindir),g'		\
	-e 's,@''exe_file@,$@.exe,g'

$(bin_SCRIPTS): script.in Makefile.am
	$(REWRITE) $(srcdir)/$< > $@.tmp
	mv $@.tmp $@

