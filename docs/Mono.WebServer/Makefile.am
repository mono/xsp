DOCS_ARCHIVE_BASE=Mono.WebServer

DOCS_ARCHIVE=$(DOCS_ARCHIVE_BASE).zip
DOCS_TREE=$(DOCS_ARCHIVE_BASE).tree
DOCS_SOURCE=$(DOCS_ARCHIVE_BASE).source

MONO_WEBSERVER_ASSEMBLY=$(top_srcdir)/src/Mono.WebServer/4.0/Mono.WebServer2.dll

INSTALL_FILES=$(DOCS_ARCHIVE) $(DOCS_TREE) $(DOCS_SOURCE)

# FIXME: -printf is gnu-ism
DOC_FILES=$(shell find en -path '*/.svn' -prune -o -type f -printf "%p ")

EXTRA_DIST = \
	$(DOCS_SOURCE) \
	$(DOC_FILES)

CLEANFILES = $(DOCS_ARCHIVE) $(DOCS_TREE)

all-local: $(DOCS_ARCHIVE)

$(DOCS_ARCHIVE): $(DOCS_TREE)
$(DOCS_TREE): docs-stamp
	$(MDOC) assemble -o Mono.WebServer en

docs-stamp:
	touch docs-stamp

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(DOCDIR)
	$(INSTALL_DATA) $(INSTALL_FILES) $(DESTDIR)$(DOCDIR)

docs-update:
	$(MDOC) update --out=en --delete -fno-assembly-versions $(MONO_WEBSERVER_ASSEMBLY)
