#!/bin/bash

# Validate not sudo
user_id=`id -u`
if [ $user_id -eq 0 -a -z "$RUNNER_ALLOW_RUNASROOT" ]; then
    echo "Must not run interactively with sudo"
    exit 1
fi

# Change directory to the script root directory
# https://stackoverflow.com/questions/59895/getting-the-source-directory-of-a-bash-script-from-within
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
    DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
    SOURCE="$(readlink "$SOURCE")"
    [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

# Do not "cd $DIR". For localRun, the current directory is expected to be the repo location on disk.

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

while :;
do
    if [[ "$1" == "localRun" ]]; then
        "$DIR"/bin/Runner.Listener $*
    else
        "$DIR"/bin/Runner.Listener run $*
    fi
    
    returnCode=$?
    if [[ $returnCode == 0 ]]; then
        echo "Runner listener exit with 0 return code, stop the service, no retry needed."
        exit $returnCode
    elif [[ $returnCode == 1 ]]; then
        echo "Runner listener exit with terminated error, stop the service, no retry needed."
        exit $returnCode
    elif [[ $returnCode == 2 ]]; then
        echo "Runner listener exit with retryable error, re-launch runner in 5 seconds."
        safe_sleep
    elif [[ $returnCode == 3 ]]; then
        # Sleep 5 seconds to wait for the runner update process finish
        echo "Runner listener exit because of updating, re-launch runner in 5 seconds"
        safe_sleep
    elif [[ $returnCode == 4 ]]; then
        # Sleep 5 seconds to wait for the ephemeral runner update process finish
        echo "Runner listener exit because of updating, re-launch ephemeral runner in 5 seconds"
        safe_sleep
    else
        exit $returnCode
    fi
done