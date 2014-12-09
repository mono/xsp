MCSFLAGS= -debug+ -debug:full -nologo

if NET_4_0
scripts4 = dbsessmgr4.exe
data4 = dbsessmgr4.exe.config
dbsessmgr4dir = $(prefix)/lib/xsp/4.0
dbsessmgr4_SCRIPTS = $(scripts4)
dbsessmgr4_DATA = $(data4)
endif

EXTRA_DIST = $(dbsessmgr_input) dbsessmgr4.exe.config
CLEANFILES = *.exe *.mdb

dbsessmgr_input = AssemblyInfo.cs.in \
		 dbsessmgr.cs

dbsessmgr_sources = $(dbsessmgr_input:.in=)

if NET_4_0
dbsessmgr4.exe: $(dbsessmgr_sources)
	$(DMCS) $(MCSFLAGS) -r:System.Data.dll -r:System.Configuration.dll -out:$@ $^
endif
