MCSFLAGS= -debug+ -debug:full -nologo

scripts2 = asp-state2.exe
data2 = asp-state2.exe.config
aspstate2dir = $(prefix)/lib/xsp/2.0
aspstate2_SCRIPTS = $(scripts2)
aspstate2_DATA = $(data2)

if NET_4_0
scripts4 = asp-state4.exe
data4 = asp-state4.exe.config
aspstate4dir = $(prefix)/lib/xsp/4.0
aspstate4_SCRIPTS = $(scripts4)
aspstate4_DATA = $(data4)
endif

EXTRA_DIST = $(aspstate_input) asp-state2.exe.config asp-state4.exe.config
CLEANFILES = *.exe *.mdb

aspstate_input = AssemblyInfo.cs.in \
		 asp_state.cs

aspstate_sources = $(aspstate_input:.in=)

asp-state2.exe: $(aspstate_sources)
	$(GMCS) $(MCSFLAGS) /out:$@ $^

if NET_4_0
asp-state4.exe: $(aspstate_sources)
	$(DMCS) $(MCSFLAGS) /out:$@ $^
endif
