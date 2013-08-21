#!/bin/bash

../gen.sh || exit 1

# Create configs
for i in {1..10}
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
done
