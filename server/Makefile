# Run make CSC=csc if you want debugging on windows.
# Add CSCFLAGS=/d:TRACE if you want trace support
CSC=mcs
CSCFLAGS+= /debug+ /debug:full /nologo

# 
REFERENCES= System.Web
REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

SOURCES = server.cs \
	  MonoApplicationHost.cs \
	  MonoWorkerRequest.cs \
	  Tracing.cs

all: server.exe
	
install: server.exe web.config global.asax
	rm -rf test
	mkdir -p test/bin
	cp server.exe web.config global.asax test
	cp server.exe test/bin
	cp ../test/*.aspx ../test/*.xml ../test/*.png test
	cp ../test/*.dll test/bin

server.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) $(REFS) /out:$@ $^

trace: 
	rm -f server.exe
	make -C . CSCFLAGS=/d:WEBTRACE CSC=$(CSC)

clean:
	rm -f server.exe *~ *.pdb
	rm -rf test

