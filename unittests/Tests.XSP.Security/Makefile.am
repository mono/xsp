noinst_SCRIPTS = Tests.XSP.Security.dll

security_test_sources = \
	$(srcdir)/DirectoryTraversalTest.cs	\
	$(srcdir)/FileExtensionsTest.cs		\
	$(srcdir)/WebTestHelper.cs

CLEANFILES = Tests.XSP.Security.dll*
EXTRA_DIST = $(security_test_sources) 

Tests.XSP.Security.dll: $(security_test_sources)
	mcs -debug -pkg:mono-nunit -out:$@ -t:library $(security_test_sources)

run-test: Tests.XSP.Security.dll
	nunit-console Tests.XSP.Security.dll

run-test-local: Tests.XSP.Security.dll
	XSP_TEST_HOST=localhost:8080 nunit-console Tests.XSP.Security.dll

