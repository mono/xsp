#!/bin/sh

CMD=$1
shift
OUTPUT_FILE=$1
shift

${CMD} $@ > ${OUTPUT_FILE}

