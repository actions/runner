#!/bin/bash

# Change directory to the script root directory
# https://stackoverflow.com/questions/59895/getting-the-source-directory-of-a-bash-script-from-within
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
    DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
    SOURCE="$(readlink "$SOURCE")"
    [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

run() {
    # run the helper process which keep the listener alive
    while :;
    do
        cp -f "$DIR"/run-helper.sh.template "$DIR"/run-helper.sh
        "$DIR"/run-helper.sh $*
        returnCode=$?
        if [[ $returnCode -eq 2 ]]; then
            echo "Restarting runner..."
        else
            echo "Exiting runner..."
            exit 0
        fi
    done
}

handle_sigterm() {
    # Default graceful stop timeout is 3 seconds
    RUNNER_GRACEFUL_STOP_TIMEOUT=${RUNNER_GRACEFUL_STOP_TIMEOUT:-3}
    echo "Received SIGTERM, " \
        "Graceful shutdown in $RUNNER_GRACEFUL_STOP_TIMEOUT Secs ..."

    if [ -n "$RUNNER_TOKEN" ]; then
        idle_runner="/runner/config.sh remove --token $RUNNER_TOKEN"
    else
        # workaround for Issue#3330
        # For the case JITCONFIG is used instead of reg token.
        # Fallback to check if worker is running,race condition prone.
        worker_process_id=$(pgrep Runner.Worker)
        idle_runner="test -z \"$worker_process_id\""
    fi

    if ! eval $idle_runner; then
        echo "Running a job, waiting for $RUNNER_GRACEFUL_STOP_TIMEOUT s to finish.."
        i=0
        while [[ $i -lt $RUNNER_GRACEFUL_STOP_TIMEOUT ]]; do
            # While waiting, if runner is stops.
            if ! ps -p $PID > /dev/null; then
                echo "Runner stopped itself while graceful period waiting."
                return
            fi
            sleep 1
            ((i++))
        done
        echo "Graceful period over, terminating..."
    fi
    if ps -p $PID > /dev/null; then
        kill -INT -$PID
    fi
}

runWithManualTrap() {
    # Set job control
    set -m

    trap 'handle_sigterm' TERM
    trap 'kill -INT -$PID' INT

    # run the helper process which keep the listener alive
    while :;
    do
        cp -f "$DIR"/run-helper.sh.template "$DIR"/run-helper.sh
        "$DIR"/run-helper.sh $* &
        PID=$!
        wait $PID
        returnCode=$?
        if [[ $returnCode -eq 2 ]]; then
            echo "Restarting runner..."
        else
            echo "Exiting runner..."
            # Unregister signal handling before exit
            trap - INT TERM
            # wait for last parts to be logged
            wait $PID
            exit $returnCode
        fi
    done
}

function updateCerts() {
    local sudo_prefix=""
    local user_id=`id -u`

    if [ $user_id -ne 0 ]; then
        if [[ ! -x "$(command -v sudo)" ]]; then
            echo "Warning: failed to update certificate store: sudo is required but not found"
            return 1
        else
            sudo_prefix="sudo"
        fi
    fi

    if [[ -x "$(command -v update-ca-certificates)" ]]; then
        eval $sudo_prefix "update-ca-certificates"
    elif [[ -x "$(command -v update-ca-trust)" ]]; then
        eval $sudo_prefix "update-ca-trust"
    else
        echo "Warning: failed to update certificate store: update-ca-certificates or update-ca-trust not found. This can happen if you're using a different runner base image."
        return 1
    fi
}

if [[ ! -z "$RUNNER_UPDATE_CA_CERTS" ]]; then
    updateCerts
fi

if [[ -n "$RUNNER_MANUALLY_TRAP_SIG" || -n "$RUNNER_GRACEFUL_STOP_TIMEOUT" ]]; then
    runWithManualTrap $*
else
    run $*
fi
