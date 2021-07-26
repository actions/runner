#/bin/bash

valid_flag_pattern='(\ |^)-s\ |(\ |^)--scope\ |(\ |^)-[Ss]cope\ '
if [[ $* =~ $valid_flag_pattern ]]; then
echo flagland
while [ $# -ne 0 ]
do
    name="$1"
    case "$name" in
        -s|--scope|-[Ss]cope)
            shift
            runner_scope=$1
            ;;
        -g|--ghe_domain|-[Gg]he_domain)
            shift
            ghe_hostname=$1
            ;;
        -n|--name|-[Nn]ame)
            shift
            runner_name=$1
            ;;
        -u|--user|-[Uu]ser)
            shift
            svc_user=$1
            ;;
        -l|--labels|-[Ll]abels)
            shift
            labels=$1
            ;;
    esac
    shift
done
else
echo nogland
    # process indexed args for backwards compatibility
    runner_scope=${1}
    ghe_hostname=${2}
    runner_name=${3:-$(hostname)}
    svc_user=${4:-$USER}
    labels=${5}
fi

echo $runner_scope
echo $ghe_hostname
echo $runner_name
echo $svc_user
echo $labels