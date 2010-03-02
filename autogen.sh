aclocal
automake -a --foreign
autoconf
./configure $*
