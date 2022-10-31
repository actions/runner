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

runWithManualTrap() {
    # Set job control
    set -m

    trap 'kill -INT -$PID' INT TERM

    # run the helper process which keep the listener alive
    while :;
    do
        cp -f "$DIR"/run-helper.sh.template "$DIR"/run-helper.sh
        "$DIR"/run-helper.sh $* &
        PID=$!
        wait -f $PID
        returnCode=$?
        if [[ $returnCode -eq 2 ]]; then
            echo "Restarting runner..."
        else
            echo "Exiting runner..."
            # Unregister signal handling before exit
            trap - INT TERM
            # wait for last parts to be logged
            wait $PID
            exit 0
        fi
    done
}

if [[ -z "$RUNNER_MANUALLY_TRAP_SIG" ]]; then
   run
else
    runWithManualTrap
fi