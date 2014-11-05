DOCS_ARCHIVE_BASE=Mono.FastCGI

DOCS_ARCHIVE=$(DOCS_ARCHIVE_BASE).zip
DOCS_TREE=$(DOCS_ARCHIVE_BASE).tree
DOCS_SOURCE=$(DOCS_ARCHIVE_BASE).source

MONO_FASTCGI_ASSEMBLY=$(top_srcdir)/src/Mono.WebServer.FastCgi/fastcgi-mono-server4.exe

INSTALL_FILES=$(DOCS_ARCHIVE) $(DOCS_TREE) $(DOCS_SOURCE)

DOC_FILES=$(shell find en -path '*/.svn' -prune -o -type f -printf "%p ")

EXTRA_DIST = \
	$(DOCS_SOURCE) \
	$(DOC_FILES)

CLEANFILES = $(DOCS_ARCHIVE) $(DOCS_TREE)

all-local: $(DOCS_ARCHIVE)

$(DOCS_ARCHIVE): $(DOCS_TREE)
$(DOCS_TREE): docs-stamp
	$(MDOC) assemble -o Mono.FastCGI en

docs-stamp:
	touch docs-stamp

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(DOCDIR)
	$(INSTALL_DATA) $(INSTALL_FILES) $(DESTDIR)$(DOCDIR)

docs-update:
	$(MDOC) update --out=en --delete -fno-assembly-versions $(MONO_FASTCGI_ASSEMBLY)
