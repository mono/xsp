SUBDIRS = AspTester HtmlTester

MCSFLAGS= -debug+ -debug:full -nologo -lib:../NUnitAsp

nunitasptestdir = $(libdir)
nunitasptest_SCRIPTS = NUnitAspTest.dll

EXTRA_DIST = $(local_sources) NUnitAspTest.csproj
CLEANFILES = NUnitAspTest.dll

# 

asptesterdir_sources = \
	./AspTester/ButtonTest.cs \
	./AspTester/CheckBoxTest.cs \
	./AspTester/DataGridTest.cs \
	./AspTester/DropDownListTest.cs \
	./AspTester/ImageButtonTest.cs \
	./AspTester/LabelTest.cs \
	./AspTester/LinkButtonTest.cs \
	./AspTester/ListBoxTest.cs \
	./AspTester/ListControlTest.cs \
	./AspTester/ListItemCollectionTest.cs \
	./AspTester/RadioButtonTest.cs \
	./AspTester/TextBoxTest.cs \
	./AspTester/UserControlTest.cs \
	./AspTester/ValidationSummaryTest.cs

htmltesterdir_sources = \
	./HtmlTester/AnchorTest.cs \
	./HtmlTester/HtmlInputCheckBoxTest.cs

local_sources = \
	./AssemblyInfo.cs \
	./HttpClientCredentialsTest.cs \
	./HttpClientTest.cs \
	./NUnitAspTestCase.cs \
	./ParserTest.cs \
	./PerformanceTest.cs \
	./SetupTest.cs \
	./WebAssertionTest.cs

nunitasptest_sources = \
	$(local_sources) \
	$(asptesterdir_sources) \
	$(htmltesterdir_sources)

NUnitAspTest.dll: $(nunitasptest_sources)
	$(MCS) $(MCSFLAGS) -r:System.Web.dll -lib:../AspTest \
		-r NUnitAsp.dll -r NUnit.Framework.dll \
		-r System.DirectoryServices \
		-target:library -out:$@ $^

