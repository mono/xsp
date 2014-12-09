testfiles = \
		populate-on-demand.aspx \
		populate.cs \
		treeview.aspx \
		treeview-databound.aspx \
		index.aspx

treeview_src = populate.cs
treeview_build = $(addprefix $(srcdir)/, $(treeview_src))

samplesdir = $(pkglibdir)/test/2.0/treeview
samples_DATA = $(testfiles)

samplesbindir = $(pkglibdir)/test/bin
samplesbin_SCRIPTS = treeview.dll

EXTRA_DIST = $(testfiles) \
	         $(treeview_src)

CLEANFILES = $(samplesbin_SCRIPTS)

treeview.dll: $(treeview_build)
	$(DMCS) -r:System.Web.dll -t:library -out:$@ $^

dist-hook:
	$(mkinstalldirs) $(distdir)/TreeLineImages
	cp -p $(srcdir)/TreeLineImages/*gif $(distdir)/TreeLineImages

install-data-hook:
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)/test/2.0/treeview/TreeLineImages
	cp $(srcdir)/TreeLineImages/*gif $(DESTDIR)$(pkglibdir)/test/2.0/treeview/TreeLineImages

uninstall-hook:
	rm -f $(DESTDIR)$(pkglibdir)/test/2.0/treeview/TreeLineImages/*gif

