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
	./AspTester/AspControlTester.cs \
	./AspTester/ButtonTester.cs \
	./AspTester/CheckBoxTester.cs \
	./AspTester/DataGridTester.cs \
	./AspTester/DropDownListTester.cs \
	./AspTester/ImageButtonTester.cs \
	./AspTester/LabelTester.cs \
	./AspTester/LinkButtonTester.cs \
	./AspTester/ListBoxTester.cs \
	./AspTester/ListControlTester.cs \
	./AspTester/ListItemCollectionTester.cs \
	./AspTester/ListItemTester.cs \
	./AspTester/PanelTester.cs \
	./AspTester/RadioButtonTester.cs \
	./AspTester/TextBoxTester.cs \
	./AspTester/UserControlTester.cs \
	./AspTester/ValidationSummaryTester.cs

htmltesterdir_sources = \
	./HtmlTester/AnchorTester.cs \
	./HtmlTester/HtmlControlTester.cs \
	./HtmlTester/HtmlInputCheckBoxTester.cs

sgmldir_sources = \
	./Sgml/SgmlParser.cs \
	./Sgml/SgmlReader.cs

local_sources =  \
	./AssemblyInfo.cs \
	./ControlTester.cs \
	./HttpClient.cs \
	./Tester.cs \
	./WebAssertion.cs \
	./WebForm.cs \
	./WebFormTestCase.cs \
	./WebPage.cs

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
	$(MCS) $(MCSFLAGS) -r:System.Web.dll -r:NUnit.Framework.dll \
		-resource:"$(htmldtd)",NUnit.Extensions.Asp.Sgml.Html.dtd \
		-target:library -out:$@ $(files)

