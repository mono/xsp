all: 
	make -C test
	make -C server

install: all
	make -C test
	make -C server install
	@echo ""
	@echo Now cd to server/test and run: mono server.exe or ./server.exe
	@echo Then point your web browser to http://127.0.0.1:8080/
	@echo Enjoy!

old:
	rm -rf rundir
	-mkdir -p rundir/output
	make -C test
	make -C src
	cp src/xsp.exe src/HttpServer/server.exe rundir
	cp test/*.aspx test/*.png test/*.xml rundir
	cp test/*.dll rundir/output
	@echo ""
	@echo Now cd to rundir and run: mono server.exe 8000
	@echo Then point your web browser to http://127.0.0.1:8000/
	@echo Enjoy!

clean:
	make -C test clean
	make -C server clean
	make -C src clean
	rm -rf rundir *~

