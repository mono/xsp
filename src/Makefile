CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full  /optimize

SOURCES = asp-parser.cs asp-tokenizer.cs aspelements.cs driver.cs generator.cs

all: aspparser.exe

aspparser.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /r:System.Web.dll /out:aspparser.exe ${SOURCES}

clean:
	rm -f aspparser.exe *~ *.pdb
	

