MCSFLAGS= -debug+ -debug:full -nologo

dbsessmgrdir = $(libdir)/mono/1.0
dbsessmgr_SCRIPTS = dbsessmgr.exe dbsessmgr.exe.config

if NET_2_0
scripts2 = dbsessmgr2.exe dbsessmgr2.exe.config
endif
dbsessmgr2dir = $(libdir)/mono/2.0
dbsessmgr2_SCRIPTS = $(scripts2)

EXTRA_DIST = $(dbsessmgr_input) dbsessmgr.exe.config dbsessmgr2.exe.config
CLEANFILES = *.exe *.mdb

dbsessmgr_input = AssemblyInfo.cs.in \
		 dbsessmgr.cs

dbsessmgr_sources = $(dbsessmgr_input:.in=)

dbsessmgr.exe: $(dbsessmgr_sources)
	$(MCS) $(MCSFLAGS) -r:System.Data.dll -out:$@ $^

dbsessmgr2.exe: $(dbsessmgr_sources)
	$(GMCS) $(MCSFLAGS) -r:System.Data.dll -out:$@ $^

