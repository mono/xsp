ifdef MONO
_OUT=-o 
CSC=mcs
CSCFLAGS= $(REFS) --define MONO
REFS= -r System.Web -r System.Drawing
LIBTARGET=--target library
else
_OUT=/out:
CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
LIBTARGET=/target:library
ifdef DEBUGMONOSW
REFERENCES= .\\System.Web		\
	    System.Drawing
SYSTEM_WEB=System.Web.dll
else
REFERENCES= System.Web \
	    System.Drawing
SYSTEM_WEB= remove_system_web
endif
REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))
endif

SOURCES = asp-parser.cs \
	  asp-tokenizer.cs \
	  aspelements.cs \
	  driver.cs \
	  generator.cs

all:  $(SYSTEM_WEB) MyForm.dll xsp.exe

xsp.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) $(_OUT)$@ $^

MyForm.dll: MyForm.cs $(SYSTEM_WEB)
	$(CSC) $(CSCFLAGS) $(LIBTARGET) $(_OUT)$@ $<

remove_system_web:
	rm -f System.Web.dll

System.Web.dll:
	echo "You must copy mono System.Web.dll to this directory"

clean:
	rm -f MyForm.dll xsp.exe *~ *.pdb 

