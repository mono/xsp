MCSFLAGS= -debug+ -debug:full -nologo

dbsessmgrdir = $(libdir)/mono/1.0
dbsessmgr_SCRIPTS = dbsessmgr.exe dbsessmgr.exe.config

EXTRA_DIST = $(dbsessmgr_input) dbsessmgr.exe.config
CLEANFILES = dbsessmgr.exe dbsessmgr.exe.mdb

dbsessmgr_input = AssemblyInfo.cs.in \
		 dbsessmgr.cs

dbsessmgr_sources = $(dbsessmgr_input:.in=)

dbsessmgr.exe: $(dbsessmgr_sources)
	$(MCS) $(MCSFLAGS) -r:System.Data.dll -out:$@ $^

