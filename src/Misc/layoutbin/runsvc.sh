#!/bin/bash

if [ -f ".Path" ]; then
    # configure
    export PATH=`cat .Path`
    echo ".Path=${PATH}"
fi

# insert anything to setup env when running as a service

# run the host process which keep the listener alive
./externals/node/bin/node ./bin/AgentService.js
