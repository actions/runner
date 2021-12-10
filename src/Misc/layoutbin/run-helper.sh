#!/bin/bash

# Validate not sudo
user_id=`id -u`
if [ $user_id -eq 0 -a -z "$RUNNER_ALLOW_RUNASROOT" ]; then
    echo "Must not run interactively with sudo"
    exit 1
fi

# Run
shopt -s nocasematch

safe_sleep() {
    if [ ! -x "$(command -v sleep)" ]; then
        if [ ! -x "$(command -v ping)" ]; then
            COUNT="0"
            while [[ $COUNT != 5000 ]]; do
                echo "SLEEP" > /dev/null
                COUNT=$[$COUNT+1]
            done
        else
            ping -c 5 127.0.0.1 > /dev/null
        fi
    else
        sleep 5
    fi
}

bin/Runner.Listener run $*
returnCode=$?
if [[ $returnCode == 0 ]]; then
    echo "Runner listener exit with 0 return code, stop the service, no retry needed."
    exit 0
elif [[ $returnCode == 1 ]]; then
    echo "Runner listener exit with terminated error, stop the service, no retry needed."
    exit 0
elif [[ $returnCode == 2 ]]; then
    echo "Runner listener exit with retryable error, re-launch runner in 5 seconds."
    safe_sleep
    exit 1
elif [[ $returnCode == 3 ]]; then
    # Sleep 5 seconds to wait for the runner update process finish
    echo "Runner listener exit because of updating, re-launch runner in 5 seconds"
    safe_sleep
    exit 1
elif [[ $returnCode == 4 ]]; then
    # Sleep 5 seconds to wait for the ephemeral runner update process finish
    echo "Runner listener exit because of updating, re-launch ephemeral runner in 5 seconds"
    safe_sleep
    exit 1
else
    echo "Exiting with unknown error code: ${returnCode}"
    exit 0
fi
