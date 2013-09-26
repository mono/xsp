#!/bin/bash

. vars.sh
. functions.sh

checkuser "fpm" "" || exit 1

checkdir $CONFIGDIR $CONFIGDIR_PERM $CONFIGDIR_GROUP
checkdir $SOCKDIR   $SOCKDIR_PERM   $SOCKDIR_GROUP
checkdir $SHIMDIR   $SHIMDIR_PERM   $SHIMDIR_GROUP
checkdir $BACKDIR   $BACKDIR_PERM   $BACKDIR_GROUP
checkdir $WEBDIR    $WEBDIR_PERM    $WEBDIR_GROUP

diff template  "$CONFIGDIR/template" > /dev/null 2> /dev/null
if [ $? == 1 -o ! -f "$CONFIGDIR/template" ]; then
    sudo cp template $CONFIGDIR
fi

cd $CONFIGDIR

# Create configs
for i in {1..9}
do
    USER=user$i
    XML=$USER.xml
    
    if [ ! -h "$XML" ]; then
        sudo ln -s template $XML
    fi

    checkuser $USER "-N" || continue

    USERDIR=$WEBDIR/$USER
    if [ ! -d "$USERDIR" ]; then
        sudo mkdir -p $USERDIR
        sudo chown $USER $USERDIR
        sudo chmod 750 $USERDIR
    fi
    if [ ! -f "$USERDIR/index.aspx" ]; then
        echo -e "<p>$USER</p>\n<p><%=\"Test\"%></p>" | sudo tee $USERDIR/index.aspx > /dev/null
        sudo chown $USER $USERDIR/index.aspx
    fi
done

cd - > /dev/null
