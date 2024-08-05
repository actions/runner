#!/bin/bash

# Validate not sudo
user_id=`id -u`
if [ $user_id -eq 0 -a -z "$RUNNER_ALLOW_RUNASROOT" ]; then
    echo "Must not run interactively with sudo"
    exit 1
fi

# Run
shopt -s nocasematch

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
    DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
    SOURCE="$(readlink "$SOURCE")"
    [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

# Wait for docker to start
if [ ! -z "$RUNNER_WAIT_FOR_DOCKER_IN_SECONDS" ]; then
    if [ "$RUNNER_WAIT_FOR_DOCKER_IN_SECONDS" -gt 0 ]; then
        echo "Waiting for docker to be ready."
        for i in $(seq "$RUNNER_WAIT_FOR_DOCKER_IN_SECONDS"); do
            if docker ps > /dev/null 2>&1; then
                echo "Docker is ready."
                break
            fi
            "$DIR"/safe_sleep.sh 1
        done
    fi
fi

updateFile="update.finished"
"$DIR"/bin/Runner.Listener run $*

returnCode=$?
if [[ $returnCode == 0 ]]; then
    echo "Runner listener exit with 0 return code, stop the service, no retry needed."
    exit 0
elif [[ $returnCode == 1 ]]; then
    echo "Runner listener exit with terminated error, stop the service, no retry needed."
    exit 0
elif [[ $returnCode == 2 ]]; then
    echo "Runner listener exit with retryable error, re-launch runner in 5 seconds."
    "$DIR"/safe_sleep.sh 5
    exit 2
elif [[ $returnCode == 3 ]]; then
    # Wait for 30 seconds or for flag file to exists for the runner update process finish
    echo "Runner listener exit because of updating, re-launch runner after successful update"
    for i in {0..30}; do
        if test -f "$updateFile"; then
            echo "Update finished successfully."
            rm "$updateFile"
            break
        fi
        "$DIR"/safe_sleep.sh 1
    done
    exit 2
elif [[ $returnCode == 4 ]]; then
    # Wait for 30 seconds or for flag file to exists for the ephemeral runner update process finish
    echo "Runner listener exit because of updating, re-launch runner after successful update"
    for i in {0..30}; do
        if test -f "$updateFile"; then
            echo "Update finished successfully."
            rm "$updateFile"
            break
        fi
        "$DIR"/safe_sleep.sh 1
    done
    exit 2
elif [[ $returnCode == 5 ]]; then
    echo "Runner listener exit with Session Conflict error, stop the service, no retry needed."
    exit 0
else
    echo "Exiting with unknown error code: ${returnCode}"
    exit 0
fi
