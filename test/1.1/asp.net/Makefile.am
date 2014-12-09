samplesdir = $(pkglibdir)/test/1.1/asp.net

testfiles = \
		browsercaps.aspx \
		codebehind1.aspx \
		code-render.aspx \
		body.inc \
		header.inc \
		registertest1.ascx \
		registertest2.ascx \
		includetest.aspx \
		registertest.aspx \
		server-side-object.aspx \
		session1.aspx \
		typedesc.aspx \
		index.aspx \
		transfer1.aspx \
		transfer2.aspx \
		serial.aspx

typedesc_src = typedesc.cs
codebehind_src = codebehind1.cs

typedesc_build = $(addprefix $(srcdir)/, $(typedesc_src))
codebehind_build = $(addprefix $(srcdir)/, $(codebehind_src))

samplesbindir = $(pkglibdir)/test/bin
samplesbin_SCRIPTS = codebehind1.dll typedesc.dll

samples_DATA = $(testfiles)
EXTRA_DIST = $(testfiles) \
	     $(codebehind_src) \
	     $(typedesc_src)

CLEANFILES = $(samplesbin_SCRIPTS)

%.dll: %.cs
	$(DMCS) -r:System.Web.dll -t:library -out:$@ $^

typedesc.dll: $(typedesc_build)

codebehind1.dll: $(codebehind_build)
