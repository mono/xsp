CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
ifdef DEBUGMONOSW
REFERENCES= .\\System.Web
SYSTEM_WEB=System.Web.dll
else
REFERENCES= System.Web
SYSTEM_WEB= remove_system_web
endif

SOURCES = server.cs

# --------------------------------- #

REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

all:  $(SYSTEM_WEB) server.exe

server.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

remove_system_web:
	rm -f System.Web.dll

System.Web.dll:
	echo "You must copy mono System.Web.dll to this directory"

clean:
	rm -f server.exe *~ *.pdb

