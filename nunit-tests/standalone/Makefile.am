MCSFLAGS= -debug+ -debug:full -nologo \
	   -lib:@top_srcdir@/tools/nunitasp/source/NUnitAsp \
	   -lib:"$(libdir)"


DLLNAME = standalone-tests.dll

EXTRA_DIST = $(aspnet_files) $(cs_files)
CLEANFILES = $(DLLNAME) $(DLLNAME).mdb

aspnet_files = \
	test1.aspx \
	test2.aspx \
	test3.aspx \
	test4.aspx

cs_files = test1.cs

# 
REFS= -r:System.Web.dll -r:nunit.framework.dll \
	-r:NUnitAsp.dll -r:nunit.core.dll -r:nunit.util.dll

all: $(DLLNAME)

$(DLLNAME): $(cs_files)
	$(MCS) $(MCSFLAGS) $(REFS) -target:library -out:$@ $^

