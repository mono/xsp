SUBDIRS = AspTester HtmlTester Sgml

if PLATFORM_WIN32
MCSFLAGS= -debug+ -debug:full -nologo -lib:"$(libdir)"
else
MCSFLAGS= -debug+ -debug:full -nologo
endif
nunitaspdir = $(libdir)
nunitasp_SCRIPTS = NUnitAsp.dll

EXTRA_DIST = $(local_sources) NUnitAsp.csproj
CLEANFILES = NUnitAsp.dll

asptesterdir_sources = \
	$(srcdir)/AspTester/AspControlTester.cs \
	$(srcdir)/AspTester/ButtonTester.cs \
	$(srcdir)/AspTester/CheckBoxTester.cs \
	$(srcdir)/AspTester/DataGridTester.cs \
	$(srcdir)/AspTester/DropDownListTester.cs \
	$(srcdir)/AspTester/ImageButtonTester.cs \
	$(srcdir)/AspTester/LabelTester.cs \
	$(srcdir)/AspTester/LinkButtonTester.cs \
	$(srcdir)/AspTester/ListBoxTester.cs \
	$(srcdir)/AspTester/ListControlTester.cs \
	$(srcdir)/AspTester/ListItemCollectionTester.cs \
	$(srcdir)/AspTester/ListItemTester.cs \
	$(srcdir)/AspTester/PanelTester.cs \
	$(srcdir)/AspTester/RadioButtonTester.cs \
	$(srcdir)/AspTester/TextBoxTester.cs \
	$(srcdir)/AspTester/UserControlTester.cs \
	$(srcdir)/AspTester/ValidationSummaryTester.cs

htmltesterdir_sources = \
	$(srcdir)/HtmlTester/AnchorTester.cs \
	$(srcdir)/HtmlTester/HtmlControlTester.cs \
	$(srcdir)/HtmlTester/HtmlInputCheckBoxTester.cs

sgmldir_sources = \
	$(srcdir)/Sgml/SgmlParser.cs \
	$(srcdir)/Sgml/SgmlReader.cs

local_sources =  \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/ControlTester.cs \
	$(srcdir)/HttpClient.cs \
	$(srcdir)/Tester.cs \
	$(srcdir)/WebAssertion.cs \
	$(srcdir)/WebForm.cs \
	$(srcdir)/WebFormTestCase.cs \
	$(srcdir)/WebPage.cs

nunitasp_sources = \
	$(asptesterdir_sources) \
	$(htmltesterdir_sources) \
	$(sgmldir_sources) \
	$(local_sources)

htmldtd = $(srcdir)/Sgml/Html.dtd

if PLATFORM_WIN32
files=$(subst /,\\,$(nunitasp_sources))
else
files=$(nunitasp_sources)
endif

NUnitAsp.dll: $(nunitasp_sources)
	$(MCS) $(MCSFLAGS) -r:System.Web.dll -r:nunit.framework.dll \
		-resource:"$(htmldtd)",NUnit.Extensions.Asp.Sgml.Html.dtd \
		-target:library -out:$@ $(files)

