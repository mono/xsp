CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
REFERENCES= System.Web		\
	    System.Drawing

SOURCES = asp-parser.cs \
	  asp-tokenizer.cs \
	  aspelements.cs \
	  driver.cs \
	  generator.cs

REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

all: MyForm.dll xsp.exe

xsp.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

MyForm.dll: MyForm.cs
	$(CSC) $(CSCFLAGS) /t:library $<

clean:
	rm -f MyForm.dll xsp.exe *~ *.pdb 

