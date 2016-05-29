#!/bin/bash

varCheckList=('LANG' 'JAVA_HOME' 'ANT_HOME' 'M2_HOME' 'ANDROID_HOME' 'GRADLE_HOME')
envContents=""

if [ -f ".Env" ]; then
    envContents=`cat .Env`
else
    touch .Env
fi

function writeVar()
{
    checkVar="$1"
    checkDelim="${1}="
    if test "${envContents#*$checkDelim}" = "$envContents"
    then
        if [ ! -z "${!checkVar}" ]; then
            echo "${checkVar}=${!checkVar}">>.Env
        fi
    fi 
}

echo $PATH>.Path

for var_name in ${varCheckList[@]}
do
    writeVar "${var_name}"
done
