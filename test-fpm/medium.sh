#!/bin/bash
export MONO_OPTIONS=--debug
export CURR=$(pwd)
sudo -i bash -c "cd $CURR; MONO_OPTIONS=--debug mono-fpm --config-dir /tmp/medium --loglevels all --verbose"
