CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
ifdef DEBUGMONOSW
REFERENCES= .\\System.Web		\
	    System.Drawing
SYSTEM_WEB=System.Web.dll
else
REFERENCES= System.Web \
	    System.Drawing
SYSTEM_WEB=
endif

SOURCES = asp-parser.cs \
	  asp-tokenizer.cs \
	  aspelements.cs \
	  driver.cs \
	  generator.cs

REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

all:  $(SYSTEM_WEB) MyForm.dll xsp.exe

xsp.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

MyForm.dll: MyForm.cs $(SYSTEM_WEB)
	$(CSC) $(CSCFLAGS) /t:library $<

System.Web.dll:
	echo "You must copy mono System.Web.dll to this directory"

clean:
	rm -f MyForm.dll xsp.exe *~ *.pdb 

