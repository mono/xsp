CSC=mcs
CSCFLAGS= $(REFS) /define:MONO
REFERENCES= System.Web

SOURCES = server.cs

# --------------------------------- #
REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

all: server.exe

server.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

clean:
	rm -f server.exe *~ *.pdb

