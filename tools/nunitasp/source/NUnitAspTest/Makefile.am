SUBDIRS = AspTester HtmlTester

if PLATFORM_WIN32
MCSFLAGS= -debug+ -debug:full -nologo -lib:.$(srcdir)/NUnitAsp -lib:"$(libdir)"
else
MCSFLAGS= -debug+ -debug:full -nologo -lib:.$(srcdir)/NUnitAsp
endif

nunitasptestdir = $(libdir)
noinst_SCRIPTS = NUnitAspTest.dll

EXTRA_DIST = $(local_sources) NUnitAspTest.csproj
CLEANFILES = NUnitAspTest.dll NUnitAspTest.dll.mdb

# 

asptesterdir_sources = \
	$(srcdir)/AspTester/ButtonTest.cs \
	$(srcdir)/AspTester/CheckBoxTest.cs \
	$(srcdir)/AspTester/DataGridTest.cs \
	$(srcdir)/AspTester/DropDownListTest.cs \
	$(srcdir)/AspTester/ImageButtonTest.cs \
	$(srcdir)/AspTester/LabelTest.cs \
	$(srcdir)/AspTester/LinkButtonTest.cs \
	$(srcdir)/AspTester/ListBoxTest.cs \
	$(srcdir)/AspTester/ListControlTest.cs \
	$(srcdir)/AspTester/ListItemCollectionTest.cs \
	$(srcdir)/AspTester/RadioButtonTest.cs \
	$(srcdir)/AspTester/TextBoxTest.cs \
	$(srcdir)/AspTester/UserControlTest.cs \
	$(srcdir)/AspTester/ValidationSummaryTest.cs

htmltesterdir_sources = \
	$(srcdir)/HtmlTester/AnchorTest.cs \
	$(srcdir)/HtmlTester/HtmlInputCheckBoxTest.cs

local_sources = \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/HttpClientCredentialsTest.cs \
	$(srcdir)/HttpClientTest.cs \
	$(srcdir)/NUnitAspTestCase.cs \
	$(srcdir)/ParserTest.cs \
	$(srcdir)/PerformanceTest.cs \
	$(srcdir)/SetupTest.cs \
	$(srcdir)/WebAssertionTest.cs

nunitasptest_sources = \
	$(local_sources) \
	$(asptesterdir_sources) \
	$(htmltesterdir_sources)

if PLATFORM_WIN32
files=$(subst /,\\,$(nunitasptest_sources))
else
files=$(nunitasptest_sources)
endif

NUnitAspTest.dll: $(nunitasptest_sources)
	$(MCS) $(MCSFLAGS) -r:System.Web.dll -lib:../AspTest \
		-r:NUnitAsp.dll -r:nunit.framework.dll \
		-r:System.DirectoryServices.dll \
		-target:library -out:$@ $(files)

