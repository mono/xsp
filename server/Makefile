# Run make CSC=csc if you want debugging on windows.
# Add CSCFLAGS=/d:TRACE if you want trace support
CSC=mcs
CSCFLAGS+= -debug+ -debug:full -nologo

# 
XSP_REFERENCES= System.Web
MODMONO_SERVER_REFERENCES= System.Web Mono.Posix
XSP_REFS= $(addsuffix .dll, $(addprefix -r:, $(XSP_REFERENCES)))
MODMONO_SERVER_REFS= $(addsuffix .dll, $(addprefix -r:, $(MODMONO_SERVER_REFERENCES)))

XSP_SOURCES = AssemblyInfo.cs \
          IApplicationHost.cs \
	  XSPApplicationHost.cs \
	  MonoWorkerRequest.cs \
	  XSPWorkerRequest.cs \
	  InitialWorkerRequest.cs \
	  Tracing.cs \
          server.cs
	  
MODMONO_SERVER_SOURCES =  IApplicationHost.cs \
			  MonoWorkerRequest.cs \
			  ModMonoRequest.cs \
			  ModMonoWorkerRequest.cs \
			  XSPApplicationHost.cs \
			  InitialWorkerRequest.cs \
			  Tracing.cs \
			  server.cs

EXES = xsp.exe mod-mono-server.exe
all: $(EXES)
	
install: $(EXES) global.asax
	mkdir -p test/bin
	cp $(EXES) xsp.exe.config mod-mono-server.exe.config global.asax test
	cp $(EXES) test/bin
	cp ../test/*.aspx ../test/*.asmx ../test/*.config ../test/*.ascx test
	cp ../test/*.xml ../test/*.xsl ../test/*.png ../test/*.inc test
	cp ../test/*.ashx test
	cp ../test/*.dll test/bin

xsp.exe: $(XSP_SOURCES)
	$(CSC) $(CSCFLAGS) $(XSP_REFS) /out:$@ $^

mod-mono-server.exe: $(MODMONO_SERVER_SOURCES)
	$(CSC) $(CSCFLAGS) $(MODMONO_SERVER_REFS) /d:MODMONO_SERVER /out:$@ $^

trace: 
	rm -f $(EXES)
	$(MAKE) -C . CSCFLAGS=/d:WEBTRACE CSC=$(CSC)

clean:
	rm -f $(EXES) *~ *.pdb
	rm -rf test

