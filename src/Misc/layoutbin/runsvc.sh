#!/bin/bash

# convert SIGTERM signal to SIGINT
# for more info on how to propagate SIGTERM to a child process see: http://veithen.github.io/2014/11/16/sigterm-propagation.html
trap 'kill -INT $PID' TERM INT

if [ -f ".path" ]; then
    # configure
    export PATH=`cat .path`
    echo ".path=${PATH}"
fi

nodever=${GITHUB_ACTIONS_RUNNER_FORCED_NODE_VERSION:-node16}

# insert anything to setup env when running as a service
# run the host process which keep the listener alive
./externals/$nodever/bin/node ./bin/RunnerService.js &
PID=$!
wait $PID
trap - TERM INT
wait $PID
