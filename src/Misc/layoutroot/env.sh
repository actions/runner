#!/bin/bash

varCheckList=(
    'LANG' 
    'JAVA_HOME' 
    'ANT_HOME' 
    'M2_HOME' 
    'ANDROID_HOME' 
    'GRADLE_HOME' 
    'NVM_BIN' 
    'NVM_PATH'
    'LD_LIBRARY_PATH'
    'PERL5LIB'
    )

envContents=""

if [ -f ".env" ]; then
    envContents=`cat .env`
else
    touch .env
fi

function writeVar()
{
    checkVar="$1"
    checkDelim="${1}="
    if test "${envContents#*$checkDelim}" = "$envContents"
    then
        if [ ! -z "${!checkVar}" ]; then
            echo "${checkVar}=${!checkVar}">>.env
        fi
    fi 
}

echo $PATH>.path

for var_name in ${varCheckList[@]}
do
    writeVar "${var_name}"
done
