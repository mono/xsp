AC_DEFUN([SHAMROCK_CHECK_MONODOC],
[
	AC_ARG_ENABLE(docs, AC_HELP_STRING([--disable-docs], 
		[Do not build documentation]), , enable_docs=yes)

	if test "x$enable_docs" = "xyes"; then
		AC_PATH_PROG(MDOC, mdoc, no)
		if test "x$MDOC" = "xno"; then
			AC_MSG_ERROR([You need to install mdoc, or pass --disable-docs to configure to skip documentation installation])
		fi

		DOCDIR=`$PKG_CONFIG monodoc --variable=sourcesdir`
		AC_SUBST(DOCDIR)
		AM_CONDITIONAL(BUILD_DOCS, true)
	else
		AC_MSG_NOTICE([not building ${PACKAGE} API documentation])
		AM_CONDITIONAL(BUILD_DOCS, false)
	fi
])

