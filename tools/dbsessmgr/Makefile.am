MCSFLAGS= -debug+ -debug:full -nologo

scripts2 = dbsessmgr2.exe
data2 = dbsessmgr2.exe.config
dbsessmgr2dir = $(prefix)/lib/xsp/2.0
dbsessmgr2_SCRIPTS = $(scripts2)
dbsessmgr2_DATA = $(data2)

if NET_4_0
scripts4 = dbsessmgr4.exe
data4 = dbsessmgr4.exe.config
dbsessmgr4dir = $(prefix)/lib/xsp/4.0
dbsessmgr4_SCRIPTS = $(scripts4)
dbsessmgr4_DATA = $(data4)
endif

EXTRA_DIST = $(dbsessmgr_input) dbsessmgr2.exe.config dbsessmgr4.exe.config
CLEANFILES = *.exe *.mdb

dbsessmgr_input = AssemblyInfo.cs.in \
		 dbsessmgr.cs

dbsessmgr_sources = $(dbsessmgr_input:.in=)

dbsessmgr2.exe: $(dbsessmgr_sources)
	$(GMCS) $(MCSFLAGS) -r:System.Data.dll -r:System.Configuration.dll -out:$@ $^

if NET_4_0
dbsessmgr4.exe: $(dbsessmgr_sources)
	$(DMCS) $(MCSFLAGS) -r:System.Data.dll -r:System.Configuration.dll -out:$@ $^
endif
