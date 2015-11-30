#                                               -*- Autoconf -*-
# Process this file with autoconf to produce a configure script.

AC_PREREQ([2.53])
AC_INIT([xsp], [4.2], [http://bugzilla.xamarin.com/])
AC_CANONICAL_SYSTEM
AC_CONFIG_MACRO_DIR([build/m4])
AM_INIT_AUTOMAKE([foreign])
AM_MAINTAINER_MODE

AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

# Checks for programs.
AC_PROG_INSTALL
AC_PROG_AWK
AC_PROG_MKDIR_P
AC_PROG_LIBTOOL

# Options
AC_ARG_ENABLE([tracing],
 AC_HELP_STRING([--enable-tracing],[enable producing tracing messages]),
 [enable_tracing=yes],
 [enable_tracing=no])

if test "x$enable_tracing" != "xno" ; then
	WEBTRACING="-define:WEBTRACE -define:TRACE"
else
	WEBTRACING=""
fi

AC_ARG_WITH([runtime],
 AC_HELP_STRING([--with-runtime],[use the specified runtime when xsp is installed]),
 [RUNTIME=$withval])

AC_ARG_WITH([unit-tests],
 AC_HELP_STRING([--with-unit-tests],[build the unit tests]),
 [UNIT_TESTS=$withval])

AC_CHECK_FUNCS(send recv bind listen accept \
 chmod unlink fork execv exit \
 getpid getuid geteuid getgid getegid setreuid setregid \
 strlen strncpy strncmp printf fprintf snprintf vprintf strftime time \
 perror,, AC_MSG_ERROR("Cannot find standard functions"))

# Mono
SHAMROCK_CHECK_MONO_MODULE(2.10.0)
SHAMROCK_FIND_MONO_4_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_FIND_PROGRAM_OR_BAIL(GACUTIL, gacutil)
SHAMROCK_FIND_PROGRAM_OR_BAIL(SN, sn)
SHAMROCK_CHECK_MONO_4_0_GAC_ASSEMBLIES([        
        System.Core
	System.Configuration
	System.Data
	System.Web
	Mono.Posix
	Mono.Security
])

SAVED_DMCS="$DMCS"

if test "x$RUNTIME" = "x" ; then
   RUNTIME="$MONO"
fi

CS="C#"
if test "x$DMCS" = "xno" ; then
	AC_MSG_ERROR([No $CS compiler found. dmcs must be installed.])
fi

AC_MSG_CHECKING([for Mono 4.0 GAC for Mono.Data.Sqlite.dll])
if test -e "$($PKG_CONFIG --variable=libdir mono)/mono/4.0/Mono.Data.Sqlite.dll" -o \
        -e "$($PKG_CONFIG --variable=prefix mono)/lib/mono/4.0/Mono.Data.Sqlite.dll"; then
	AC_MSG_RESULT([found])
	HAVE_MONO_SQLITE=yes
else
	AC_MSG_RESULT([not found])
	HAVE_MONO_SQLITE=no
fi

AM_CONDITIONAL(USE_SQLITE, test x${HAVE_MONO_SQLITE} = xyes)

SHAMROCK_CHECK_MONODOC

GACUTIL_FLAGS='-root $(DESTDIR)$(prefix)/lib'

case "$host" in
	*-*-mingw*|*-*-cygwin*)
	platform_win32=yes	
	;;
esac

AM_CONDITIONAL(UNITTESTS, test x${UNIT_TESTS} = xyes)
AM_CONDITIONAL(PLATFORM_WIN32, test x$platform_win32 = xyes)
AM_CONDITIONAL(NET_2_0, test ! x$DMCS = xno)
AM_CONDITIONAL(NET_4_0, test ! x$DMCS = xno)
AM_CONDITIONAL(XSP_ONLY, test x$platform_win32 = xyes)

# Put the version in the new required format (for svn revisions)
export VERSION
[XSP_VERSION=$($AWK 'BEGIN {
  split (ENVIRON["VERSION"] ".0.0.0", vsplit, ".")
  if(length(vsplit [1]) > 4) {
    split (substr(ENVIRON["VERSION"], 0, 4) "." substr(ENVIRON["VERSION"], 5) ".0.0", vsplit, ".")
  }
  print vsplit [1] "." vsplit [2] "." vsplit [3] "." vsplit [4]
}')]

AC_SUBST(WEBTRACING)
AC_SUBST(DMCS)
AC_SUBST(RUNTIME)
AC_SUBST(GACUTIL)
AC_SUBST(GACUTIL_FLAGS)
AC_SUBST(SN)
AC_SUBST(VERSION)
AC_SUBST(XSP_VERSION)

AC_CONFIG_FILES([Makefile
	build/Makefile
	build/m4/Makefile
	build/m4/shave/shave
	build/m4/shave/shave-libtool
	docs/Makefile
	docs/Mono.FastCGI/Makefile
	docs/Mono.WebServer/Makefile
	man/Makefile
	packaging/Makefile
	packaging/opensuse/Makefile
	scripts/Makefile
	lib/Makefile
	shim/Makefile
	src/Makefile
	src/Mono.WebServer.Apache/Makefile
	src/Mono.WebServer.FastCgi/Makefile
	src/Mono.WebServer.XSP/Makefile
	src/Mono.WebServer/Makefile
	src/Mono.WebServer.Fpm/Makefile
	test/1.1/Makefile
	test/1.1/asp.net/Makefile
	test/1.1/authtest/Makefile
	test/1.1/customcontrol/Makefile
	test/1.1/databind/Makefile
	test/1.1/handlers/Makefile
	test/1.1/html/Makefile
	test/1.1/webcontrols/Makefile
	test/1.1/webservice/Makefile
	test/2.0/Makefile
	test/2.0/gridview/Makefile
	test/2.0/masterpages/Makefile
	test/2.0/menu/Makefile
	test/2.0/treeview/Makefile
	test/App_Code/Makefile
	test/Makefile
	test/controls/Makefile
	tools/Makefile
	tools/asp_state/Makefile
	tools/dbsessmgr/Makefile
	tools/mono-asp-apps/Makefile
	unittests/Makefile
	unittests/Tests.XSP.Security/Makefile
	unittests/webroot/Makefile
	xsp.spec
	man/xsp.1
	man/dbsessmgr.1
        man/asp-state.1
	man/mono-asp-apps.1
	src/Mono.WebServer/AssemblyInfo2.cs
	src/Mono.WebServer/AssemblyInfo4.cs
	src/Mono.WebServer.Apache/AssemblyInfo.cs
	src/Mono.WebServer.FastCgi/AssemblyInfo.cs
	src/Mono.WebServer.Fpm/AssemblyInfo.cs
	src/Mono.WebServer.XSP/AssemblyInfo.cs
	src/Mono.WebServer.XSP/xsp.pc
	src/Mono.WebServer.XSP/xsp-2.pc
	src/Mono.WebServer.XSP/xsp-4.pc
	tools/asp_state/AssemblyInfo.cs
	tools/dbsessmgr/AssemblyInfo.cs
])

SHAVE_INIT([build/m4/shave], [enable])
AC_OUTPUT

TARGET_FRAMEWORKS=""

if test "x$SAVED_DMCS" = "xno"; then
   DMCS_STRING="not found"
else
   DMCS_STRING="$SAVED_DMCS"
   if test -n "$TARGET_FRAMEWORKS"; then
      TARGET_FRAMEWORKS="$TARGET_FRAMEWORKS, .NET 4.5"
   else
      TARGET_FRAMEWORKS=".NET 4.5"
   fi
fi

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install prefix:          ${prefix}
    Datadir:                 ${expanded_datadir}
    Libdir:                  ${expanded_libdir}
    Build documentation:     ${enable_docs}
    Mono compiler:           ${DMCS_STRING}
    Target frameworks:       ${TARGET_FRAMEWORKS}
    Build SQLite samples:    ${HAVE_MONO_SQLITE}
EOF
