#!/bin/bash

../gen.sh || exit 1

WEBDIR=${WEBDIR:-"/tmp/website"}
CONFIGDIR=${CONFIGDIR:-"/tmp/medium"}
CONFIGDIR_PERM=${CONFIGDIR_PERM:-"755"}

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

checkdir $CONFIGDIR $CONFIGDIR_PERM

cp template $CONFIGDIR
cd $CONFIGDIR

function checkuser {
	USR=$1

	getent passwd $USR  > /dev/null

	if [ $? != 0 ]; then
		read -p "User $USR doesn't exist, create it? [y/N]" CREATEUSER
		case $CREATEUSER in
			[Yy]* )	useradd $USR -N -M -s /sbin/nologin -d /dev/null;;
			* ) return 1;;
		esac
	fi
	
	return 0
}

# Create configs
for i in {1..9}
do
	USER=user$i
	XML=$USER.xml
	
	if [ ! -h "$XML" ]; then
		ln -s template $XML
		echo $XML
	fi

	checkuser $USER || continue

	USERDIR=$WEBDIR/$USER
	rm -f $USERDIR/index.aspx
	mkdir -p $USERDIR
	chown $USER $USERDIR
	chmod 750 $USERDIR
	echo "<p>$USER</p>" > $USERDIR/index.aspx
	echo "<p><%=\"Test\"%></p>" >> $USERDIR/index.aspx
	chown $USER $USERDIR/index.aspx
done

cd -
