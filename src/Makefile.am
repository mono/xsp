if XSP_ONLY
SUBDIRS=Mono.WebServer Mono.WebServer.XSP
else

if NET_4_0
SUBDIRS=Mono.WebServer Mono.WebServer.XSP Mono.WebServer.Apache Mono.WebServer.FastCgi Mono.WebServer.Fpm
else
SUBDIRS=Mono.WebServer Mono.WebServer.XSP Mono.WebServer.Apache Mono.WebServer.FastCgi
endif

endif

EXTRA_DIST = mono.pub mono.snk
