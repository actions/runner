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

# run the helper process which keep the listener alive
while :;
do
    "$DIR"/bin/run-helper.sh $*
    returnCode=$?
    if [[ $returnCode -ge 2 ]]; then
        echo "Restart runner after it exited with return code '${returnCode}'"
    else
        echo "Exit runner after it exited with return code '${returnCode}'"
        exit $returnCode
    fi
done