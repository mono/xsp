MCSFLAGS= -debug+ -debug:full -nologo

aspstatedir = $(bindir)
aspstate_SCRIPTS = asp_state.exe asp_state.exe.config

EXTRA_DIST = $(aspstate_input) asp_state.exe.config
CLEANFILES = asp_state.exe

aspstate_input = AssemblyInfo.cs.in \
		 asp_state.cs

aspstate_sources = $(aspstate_input:.in=)

asp_state.exe: $(aspstate_sources)
	$(MCS) $(MCSFLAGS) /out:$@ $^

