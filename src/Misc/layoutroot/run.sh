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
if [[ "$1" == "localRun" ]]; then
    "$DIR"/bin/Runner.Listener $*
else
    "$DIR"/bin/Runner.Listener run $*

# Return code 4 means the run once runner received an update message.
# Sleep 5 seconds to wait for the update process finish and run the runner again.
    returnCode=$?
    if [[ $returnCode == 4 ]]; then
        if [ ! -x "$(command -v sleep)" ]; then
            if [ ! -x "$(command -v ping)" ]; then
                COUNT="0"
                while [[ $COUNT != 5000 ]]; do
                    echo "SLEEP" >nul
                    COUNT=$[$COUNT+1]
                done
            else
                ping -n 5 127.0.0.1 >nul
            fi
        else
            sleep 5 >nul
        fi
        
        "$DIR"/bin/Runner.Listener run $*
    else
        exit $returnCode
    fi
fi
