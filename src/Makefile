CSC=mcs
CSCFLAGS= $(REFS) /define:MONO
REFERENCES= System.Web System.Drawing

# 

REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

SOURCES = asp-parser.cs \
	  asp-tokenizer.cs \
	  aspelements.cs \
	  driver.cs \
	  generator.cs

all: xsp.exe

xsp.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

clean:
	rm -f xsp.exe *~ *.pdb 

