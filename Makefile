all: 
	$(MAKE) -C test
	$(MAKE) -C server

install: all
	$(MAKE) -C test
	$(MAKE) -C server install
	@echo ""
	@echo "-------------"
	@echo 'Now cd to server/test and run: mono server.exe or ./server.exe'
	@echo 'Then point your web browser to http://127.0.0.1:8080/'
	@echo 'You can change the default port (8080) in the server.exe.config file.'
	@echo ""
	@echo "If you're gonna try the samples that use a database (such as dbpage1.aspx),"
	@echo "you may need to modify the values of DBProviderAssembly, DBConnectionType"
	@echo "and/or DbConnectionString in server.exe.config file."

	@echo ""
	@echo 'Enjoy!'
	@echo "-------------"
	@echo ""

clean:
	$(MAKE) -C test clean
	$(MAKE) -C server clean
	$(MAKE) -C src clean
	rm -rf rundir *~

