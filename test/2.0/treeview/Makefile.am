testfiles = \
		populate-on-demand.aspx \
		populate.cs \
		treeview.aspx \
		treeview-databound.aspx

treeview_src = populate.cs
treeview_build = $(addprefix $(srcdir)/, $(treeview_src))

samplesdir = $(datadir)/doc/xsp/test/2.0/treeview
samples_DATA = $(testfiles)

samplesbindir = $(datadir)/doc/xsp/test/bin
samplesbin_SCRIPTS = treeview.dll

imagesdir = $(datadir)/doc/xsp/test/2.0/treeview/TreeLineImages
images_DATA = TreeLineImages/*

EXTRA_DIST = $(testfiles) \
	         $(treeview_src) \
			 $(images_DATA)

CLEANFILES = $(samplesbin_SCRIPTS)

treeview.dll: $(treeview_build)
	$(GMCS) -r:System.Web.dll -t:library -out:$@ $^
