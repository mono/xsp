#!/bin/bash

SOCKDIR=${SOCKDIR:-"/tmp/sockets"}
SOCKDIR_PERM=${SOCKDIR_PERM:-"3733"}

SHIMDIR=${SHIMDIR:-"/tmp/shims"}
SHIMDIR_PERM=${SHIMDIR_PERM:-"3733"}

WEBDIR=${WEBDIR:-"/tmp/website"}
WEBDIR_PERM=${WEBDIR_PERM:-"3711"}

HTTPD=${HTTPD:-"nginx"}

if [ $(whoami) != "root" ]; then
    echo "You need to run this script as root."
    echo "Use 'sudo ./$(basename $0)' then enter your password when prompted."
    exit 1
fi

function checkuser {
	USR=$1

	getent passwd $USR  > /dev/null

	if [ $? != 0 ]; then
		read -p "User $USR doesn't exist, create it? [y/N]" CREATEUSER
		case $CREATEUSER in
			[Yy]* )	useradd $USR -M -s /sbin/nologin -d /dev/null;;
			* ) return 1;;
		esac
	fi
	
	return 0
}

function checkdir {
	DIR=$1
	PERM=$2
	# Create dir
	if [ ! -d $DIR ]; then
		if [ -e $DIR ]; then
			echo "$DIR exists but it's not a directory!"
			exit 1
		fi

		mkdir $DIR
		chmod $PERM $DIR
	fi

	# Fix permissions
	RPERM=$(stat -c '%a' $DIR)
	if [ $RPERM != $PERM ]; then
		read -p "Wrong permissions ($RPERM), set to $PERM? [Y/n] " FIXPERM
		FIXPERM=${FIXPERM:-y}
		if [ $FIXPERM == "y" ] || [ $FIXPERM == "n" ]; then
			chmod $PERM $DIR
		else
			return 1
		fi
	fi

	if [ $DIR == $SHIMDIR ]; then
		chgrp fpm $DIR
	else
		chgrp $HTTPD $DIR
	fi

	return 0
}

checkuser "fpm" || exit 1

checkdir $SOCKDIR $SOCKDIR_PERM
checkdir $SHIMDIR $SHIMDIR_PERM
checkdir $WEBDIR $WEBDIR_PERM
