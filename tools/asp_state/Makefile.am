MCSFLAGS= -debug+ -debug:full -nologo

aspstatedir = $(libdir)/mono/1.0
aspstate_SCRIPTS = asp-state.exe asp-state.exe.config

EXTRA_DIST = $(aspstate_input) asp-state.exe.config
CLEANFILES = asp-state.exe asp-state.exe.mdb

aspstate_input = AssemblyInfo.cs.in \
		 asp_state.cs

aspstate_sources = $(aspstate_input:.in=)

asp-state.exe: $(aspstate_sources)
	$(MCS) $(MCSFLAGS) /out:$@ $^

