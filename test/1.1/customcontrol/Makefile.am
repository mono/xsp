samplesdir = $(pkglibdir)/test/1.1/customcontrol

testfiles = \
		tabcontrol2.aspx \
		tabcontrol.aspx \
		index.aspx

tabcontrol_src = tabcontrol.cs
tabcontrol2_src = tabcontrol2.cs

tabcontrol_build = $(addprefix $(srcdir)/, $(tabcontrol_src))
tabcontrol2_build = $(addprefix $(srcdir)/, $(tabcontrol2_src))

samples_DATA = $(testfiles)

samplesbindir = $(pkglibdir)/test/bin
samplesbin_SCRIPTS = tabcontrol.dll tabcontrol2.dll

EXTRA_DIST = $(testfiles) \
	     $(tabcontrol_src) \
	     $(tabcontrol2_src)

CLEANFILES = $(samplesbin_SCRIPTS)

%.dll: %.cs
	$(DMCS) -r:System.Web.dll -t:library -out:$@ $^

tabcontrol.dll: $(tabcontrol_build)

tabcontrol2.dll: $(tabcontrol2_build)
