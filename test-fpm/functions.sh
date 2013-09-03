#!/bin/sh

function checkdir {
    DIR=$1
    PERM=$2
    GROUP=$3
    # Create dir
    if [ ! -d $DIR ]; then
        if [ -e $DIR ]; then
            echo "$DIR exists but it's not a directory!"
            exit 1
        fi

        sudo mkdir $DIR
        sudo chmod $PERM $DIR
    fi

    # Fix permissions
    RPERM=$(stat -c '%a' $DIR)
    if [ $RPERM != $PERM ]; then
        read -p "Wrong permissions ($RPERM), set to $PERM? [Y/n] " FIXPERM
        FIXPERM=${FIXPERM:-"y"}
        if [ $FIXPERM == "y" ] || [ $FIXPERM == "Y" ]; then
            sudo chmod $PERM $DIR
        else
            return 1
        fi
    fi

    if [ $(stat -c '%G' $DIR) != $GROUP ]; then
        sudo chgrp $GROUP $DIR
    fi

    return 0
}

function checkuser {
    USR=$1
    OPT=$2

    getent passwd $USR  > /dev/null

    if [ $? != 0 ]; then
        read -p "User $USR doesn't exist, create it? [y/N] " CREATEUSER
        case $CREATEUSER in
            [Yy]* ) sudo useradd $USR $OPT -M -s /sbin/nologin -d /dev/null;;
            * ) return 1;;
        esac
    fi
    
    return 0
}
