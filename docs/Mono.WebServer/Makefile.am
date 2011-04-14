DOCS_ARCHIVE_BASE=Mono.WebServer

DOCS_ARCHIVE_20=$(DOCS_ARCHIVE_BASE).zip
DOCS_TREE_20=$(DOCS_ARCHIVE_BASE).tree
DOCS_SOURCE_20=$(DOCS_ARCHIVE_BASE).source

MONO_WEBSERVER_ASSEMBLY_20=$(top_srcdir)/src/Mono.WebServer/2.0/Mono.WebServer2.dll

INSTALL_FILES=$(DOCS_ARCHIVE_20) $(DOCS_TREE_20) $(DOCS_SOURCE_20)

DOC_FILES=$(shell find en -path '*/.svn' -prune -o -type f -printf "%p ")

EXTRA_DIST = \
	$(DOCS_SOURCE_20) \
	$(DOC_FILES)

CLEAN_FILES = $(DOCS_ARCHIVE_20) $(DOCS_TREE_20)

all-local: $(DOCS_ARCHIVE_20)

$(DOCS_ARCHIVE_20): $(DOCS_TREE_20)
$(DOCS_TREE_20): docs-stamp
	$(MDOC) assemble -o Mono.WebServer en

docs-stamp:
	touch docs-stamp

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(DOCDIR)
	$(INSTALL_DATA) $(INSTALL_FILES) $(DESTDIR)$(DOCDIR)

docs-update:
	$(MDOC) update --out=en --delete -fno-assembly-versions $(MONO_WEBSERVER_ASSEMBLY_20)
