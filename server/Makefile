# Run make CSC=csc if you want debugging on windows.
# Add CSCFLAGS=/d:TRACE if you want trace support
CSC=mcs
CSCFLAGS+= -debug+ -debug:full -nologo

# 
REFERENCES= System.Web Mono.Http
REFS= $(addsuffix .dll, $(addprefix -r:, $(REFERENCES)))

SOURCES = AssemblyInfo.cs \
          IApplicationHost.cs \
	  XSPApplicationHost.cs \
	  MonoWorkerRequest.cs \
	  XSPWorkerRequest.cs \
	  Tracing.cs \
          server.cs

all: xsp.exe
	
install: xsp.exe global.asax
	rm -rf test
	mkdir -p test/bin
	cp xsp.exe xsp.exe.config global.asax test
	cp xsp.exe test/bin
	cp ../test/*.aspx ../test/*.asmx ../test/*.config ../test/*.ascx ../test/*.xml ../test/*.xsl ../test/*.png ../test/*.inc test
	cp ../test/*.dll test/bin

xsp.exe: $(SOURCES)
	$(CSC) $(CSCFLAGS) $(REFS) /out:$@ $^

trace: 
	rm -f xsp.exe
	$(MAKE) -C . CSCFLAGS=/d:WEBTRACE CSC=$(CSC)

clean:
	rm -f xsp.exe *~ *.pdb
	rm -rf test

