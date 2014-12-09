samplesdir = $(pkglibdir)/test/1.1/webservice

testfiles = \
		ConverterService.asmx \
		TestService.asmx \
		ServiceClient.exe.config  \
		CompressExtension.cs \
		index.aspx

extensions_src = TraceExtension.cs EncryptExtension.cs DumpExtension.cs
serviceclient_src = ServiceClient.cs Reference.cs

extensions_build = $(addprefix $(srcdir)/, $(extensions_src))
serviceclient_build = $(addprefix $(srcdir)/, $(serviceclient_src))


samples_DATA = $(testfiles)

samplesexedir = $(pkglibdir)/test
samplesexe_SCRIPTS = ServiceClient.exe extensions.dll

samplesbindir = $(pkglibdir)/test/bin
samplesbin_SCRIPTS = extensions.dll

CLEANFILES = $(samplesbin_SCRIPTS) $(samplesexe_SCRIPTS)

EXTRA_DIST = $(testfiles) \
	     $(extensions_src) \
	     $(serviceclient_src)

%.dll: %.cs
	$(DMCS) -r:System.Web.dll -t:library -out:$@ $^

extensions.dll: $(extensions_build)
	$(DMCS) -r:System.Web.dll -r:System.Web.Services.dll -target:library \
		-out:$@ $^

ServiceClient.exe: extensions.dll $(serviceclient_build)
	$(DMCS) -r:System.Web.dll -r:System.Web.Services.dll -r:extensions.dll \
		-out:$@ \
		$(srcdir)/ServiceClient.cs $(srcdir)/Reference.cs

