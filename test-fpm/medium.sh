#!/bin/bash
export MONO_OPTIONS=--debug
export CURR=$(pwd)
sudo -i bash -c "cd $CURR; mono-fpm --config-dir medium --loglevels all --verbose"
