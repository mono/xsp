EXES= src/xsp.exe src/HttpServer/server.exe src/HttpServer/redirector.sh

all: 
	(cd src && make)

install: all
	-mkdir rundir
	-mkdir rundir/output
	cp $(EXES) rundir/
	cp test/*.aspx rundir/
	mcs --target library -o rundir/output/tabcontrol.dll \
	    -r System.Web test/tabcontrol.cs
	mcs --target library -o rundir/output/tabcontrol2.dll \
	    -r System.Web test/tabcontrol2.cs
	@echo ""
	@echo Now cd to rundir and run: mono server.exe 8000
	@echo Then point your web browser to http://127.0.0.1:8000/
	@echo Enjoy!

clean:
	(cd src && make clean)

uninstall:
	rm -rf rundir

