MCSFLAGS= -debug+ -debug:full -nologo

aspstatedir = $(libdir)/mono/1.0
aspstate_SCRIPTS = asp-state.exe asp-state.exe.config

if NET_2_0
scripts2 = asp-state2.exe asp-state2.exe.config
endif
aspstate2dir = $(libdir)/mono/2.0
aspstate2_SCRIPTS = $(scripts2)

EXTRA_DIST = $(aspstate_input) asp-state.exe.config
CLEANFILES = asp-state.exe asp-state.exe.mdb

aspstate_input = AssemblyInfo.cs.in \
		 asp_state.cs

aspstate_sources = $(aspstate_input:.in=)

asp-state.exe: $(aspstate_sources)
	$(MCS) $(MCSFLAGS) /out:$@ $^

asp-state2.exe: $(aspstate_sources)
	$(GMCS) $(MCSFLAGS) /out:$@ $^

