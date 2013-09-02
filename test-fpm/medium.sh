#!/bin/bash
if [ ! -d /tmp/medium ]; then
	cd medium
	sudo ./gen.sh
	cd ..
fi
export MONO_OPTIONS=--debug
sudo -i bash -c "cd /tmp/medium; MONO_OPTIONS=--debug mono-fpm --config-dir . --loglevels all --verbose"
