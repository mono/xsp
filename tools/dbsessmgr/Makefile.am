MCSFLAGS= -debug+ -debug:full -nologo

scripts2 = dbsessmgr2.exe
data2 = dbsessmgr2.exe.config
dbsessmgr2dir = $(prefix)/lib/xsp/2.0
dbsessmgr2_SCRIPTS = $(scripts2)
dbsessmgr2_DATA = $(data2)

EXTRA_DIST = $(dbsessmgr_input) dbsessmgr2.exe.config
CLEANFILES = *.exe *.mdb

dbsessmgr_input = AssemblyInfo.cs.in \
		 dbsessmgr.cs

dbsessmgr_sources = $(dbsessmgr_input:.in=)

dbsessmgr2.exe: $(dbsessmgr_sources)
	$(GMCS) $(MCSFLAGS) -r:System.Data.dll -out:$@ $^

