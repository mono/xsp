MCSFLAGS= -debug+ -debug:full -nologo \
	   -lib:@top_srcdir@/tools/nunitasp/source/NUnitAsp

DLLNAME = standalone-tests.dll

EXTRA_DIST = $(aspnet_files) $(cs_files)
CLEANFILES = $(DLLNAME)

aspnet_files = \
	test1.aspx \
	test2.aspx \
	test3.aspx

cs_files = test1.cs

# 
REFS= -r:System.Web.dll -r:NUnit.Framework.dll -r:NUnitAsp.dll

all: $(DLLNAME)

$(DLLNAME): $(cs_files)
	$(MCS) $(MCSFLAGS) $(REFS) -target:library -out:$@ $^

