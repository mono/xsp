MCSFLAGS= -debug+ -debug:full -nologo

scripts2 = asp-state2.exe
data2 = asp-state2.exe.config
aspstate2dir = $(prefix)/lib/xsp/2.0
aspstate2_SCRIPTS = $(scripts2)
aspstate2_DATA = $(data2)

EXTRA_DIST = $(aspstate_input) asp-state2.exe.config
CLEANFILES = *.exe *.mdb

aspstate_input = AssemblyInfo.cs.in \
		 asp_state.cs

aspstate_sources = $(aspstate_input:.in=)

asp-state2.exe: $(aspstate_sources)
	$(GMCS) $(MCSFLAGS) /out:$@ $^

