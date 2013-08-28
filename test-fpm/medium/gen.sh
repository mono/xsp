#!/bin/bash

../gen.sh || exit 1

WEBDIR=${WEBDIR:-"/tmp/website"}

# Create configs
for i in {1..9}
do
	USER=user$i
	XML=$USER.xml
	
	if [ ! -h "$XML" ]; then
		ln -s template $XML
		echo $XML
	fi

	getent passwd $USER  > /dev/null

	if [ $? != 0 ]; then
		read -p "User $USER doesn't exist, create it? [y/N]" CREATEUSER
		case $CREATEUSER in
			[Yy]* )	useradd $USER -N -M -s /sbin/nologin -d /dev/null;;
			* ) continue;;
		esac
	fi

	USERDIR=$WEBDIR/$USER
	rm -f $USERDIR/index.aspx
	mkdir -p $USERDIR
	chown $USER $USERDIR
	chmod 750 $USERDIR
	echo "<p>$USER</p>" > $USERDIR/index.aspx
	echo "<p><%=\"Test\"%></p>" >> $USERDIR/index.aspx
	chown $USER $USERDIR/index.aspx
done
