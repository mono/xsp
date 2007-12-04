SUBDIRS = controls
if NET_2_0
SUBDIRS += 1.1 2.0 App_Code
else
SUBDIRS += 1.1
endif

testfiles = \
		mod-mono-server.exe.config \
		index.aspx \
		index2.aspx \
		xsp.exe.config \
		web.config \
		global.asax \
		mono.png \
		monobutton.png \
		mono-powered-big.png \
		small-icon.png \
		sample.webapp \
		favicon.ico \
		mono-xsp.css \
		Web.sitemap \
		missing_components.aspx

samplesdir = $(pkglibdir)/test
samples_DATA = $(testfiles)

samplesbindir = $(pkglibdir)/test/bin
samplesbin_SCRIPTS = SiteMapReader_1.1.dll

sitemapreader_build = SiteMapReader_1.1.cs

CLEANFILES = $(samplesbin_SCRIPTS)
EXTRA_DIST = $(testfiles) $(sitemapreader_build)

SiteMapReader_1.1.dll: $(sitemapreader_build)
	$(MCS) -pkg:dotnet -r:System.Xml.dll -target:library -out:$@ $^
