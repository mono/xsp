CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
REFERENCES= System.Web
SOURCES = server.cs

# --------------------------------- #

REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

all: server.exe

server.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

clean:
	rm -f server.exe *~ *.pdb

