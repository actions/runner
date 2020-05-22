#!/bin/bash

# convert SIGTERM signal to SIGINT
# for more info on how to propagate SIGTERM to a child process see: http://veithen.github.io/2014/11/16/sigterm-propagation.html
trap 'kill -INT $PID' TERM INT

if [ -f ".path" ]; then
    # configure
    export PATH=`cat .path`
    echo ".path=${PATH}"
fi

if [ -f ".debug-runner-disable-ssl-verify" ]; then
    export GIT_SSL_NO_VERIFY=true
    export NODE_TLS_REJECT_UNAUTHORIZED=0
    echo "SSL verification for Git and Node disabled"
    echo "   *** DO NOT RUN THIS IN PRODUCTION ***"
fi

# insert anything to setup env when running as a service

# run the host process which keep the listener alive
./externals/node12/bin/node ./bin/RunnerService.js &
PID=$!
wait $PID
trap - TERM INT
wait $PID
