#!/bin/bash
. vars.sh
sudo ./gen.sh
sudo -i bash -c "cd $CONFIGDIR; MONO_OPTIONS=--debug mono-fpm --config-dir . --loglevels all --verbose"
