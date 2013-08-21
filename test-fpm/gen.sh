#!/bin/bash

SOCKDIR=${SOCKDIR:-"/tmp/sockets"}
SOCKDIR_PERM=${SOCKDIR_PERM:-"3733"}

WEBDIR=${WEBDIR:-"/tmp/website"}
WEBDIR_PERM=${WEBDIR_PERM:-"755"}

if [ $(whoami) != "root" ]; then
    echo "You need to run this script as root."
    echo "Use 'sudo ./$(basename $0)' then enter your password when prompted."
    exit 1
fi

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

	return 0
}

checkdir $SOCKDIR $SOCKDIR_PERM
checkdir $WEBDIR $WEBDIR_PERM && rm -rf $WEBDIR/* && cp -r website/* $WEBDIR/
