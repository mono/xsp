ifdef MONO
_OUT=-o 
CSC=mcs
CSCFLAGS= $(REFS) --define MONO
REFS= -r System.Web
else
_OUT= /out:
CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
ifdef DEBUGMONOSW
REFERENCES= .\\System.Web
SYSTEM_WEB=System.Web.dll
else
REFERENCES= System.Web
SYSTEM_WEB= remove_system_web
endif
REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))
endif
SOURCES = server.cs

# --------------------------------- #

all:  $(SYSTEM_WEB) server.exe

server.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) $(_OUT)$@ $^

remove_system_web:
	rm -f System.Web.dll

System.Web.dll:
	echo "You must copy mono System.Web.dll to this directory"

clean:
	rm -f server.exe *~ *.pdb

