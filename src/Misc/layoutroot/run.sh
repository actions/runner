user_id=`id -u`

if [ $user_id -eq 0 ]; then
    echo "Must not run interactively with sudo"
    exit 1
fi

if [ ! -f .agent ]; then
    echo "Must configure first. Run ./config.sh"
    exit 1
fi

./bin/Agent.Listener $*
