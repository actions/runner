#!/bin/bash

user_id=`id -u`

if [ $user_id -ne 0 ]; then
    echo "Need to run with sudo privilege"
    exit 1
fi

# Determine OS type 
# Debian based OS (Debian, Ubuntu, Linux Mint) has /etc/debian_version
# Fedora based OS (Fedora, Redhat, Centos, Oracle Linux 7) has /etc/redhat-release
# SUSE based OS (OpenSUSE, SUSE Enterprise) has ID_LIKE=suse in /etc/os-release

function print_errormessage() 
{
    echo "Can't install dotnet core dependencies."
    echo "You can manually install all required dependencies base on follwoing documentation"
    echo "https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x"
}

if [ -e /etc/os-release ]
then
    echo "--------OS Information--------"
    cat /etc/os-release
    echo "------------------------------"

    if [ -e /etc/debian_version ]
    then
        echo "The current OS is Debian based"
        echo "--------Debian Version--------"
        cat /etc/debian_version
        echo "------------------------------"
        
        # prefer apt over apt-get        
        command -v apt
        if [ $? -eq 0 ]
        then
            apt update && apt install -y libunwind8 liblttng-ust0 libcurl3 libuuid1 libkrb5-3 zlib1g
            if [ $? -ne 0 ]
            then
                echo "'apt' failed with exit code '$?'"
                print_errormessage
                exit 1
            fi

            # debian 9 use libssl1.0.2
            # other debian linux use libssl1.0.0
            apt install -y libssl1.0.0 || apt install -y libssl1.0.2
            if [ $? -ne 0 ]
            then
                echo "'apt' failed with exit code '$?'"
                print_errormessage
                exit 1
            fi

            # libicu version prefer: libicu52 -> libicu55 -> libicu57 -> libicu60
            apt install -y libicu52 || apt install -y libicu55 || apt install -y libicu57 || apt install -y libicu60
            if [ $? -ne 0 ]
            then
                echo "'apt' failed with exit code '$?'"
                print_errormessage
                exit 1
            fi
        else
            command -v apt-get
            if [ $? -eq 0 ]
            then
                apt-get update && apt-get install -y libunwind8 liblttng-ust0 libcurl3 libuuid1 libkrb5-3 zlib1g
                if [ $? -ne 0 ]
                then
                    echo "'apt-get' failed with exit code '$?'"
                    print_errormessage
                    exit 1
                fi

                # debian 9 use libssl1.0.2
                # other debian linux use libssl1.0.0
                apt-get install -y libssl1.0.0 || apt install -y libssl1.0.2
                if [ $? -ne 0 ]
                then
                    echo "'apt-get' failed with exit code '$?'"
                    print_errormessage
                    exit 1
                fi

                # libicu version prefer: libicu52 -> libicu55 -> libicu57 -> libicu60
                apt-get install -y libicu52 || apt install -y libicu55 || apt install -y libicu57 || apt install -y libicu60
                if [ $? -ne 0 ]
                then
                    echo "'apt-get' failed with exit code '$?'"
                    print_errormessage
                    exit 1
                fi
            else
                echo "Can not find 'apt' or 'apt-get'"
                print_errormessage
                exit 1
            fi
        fi
    elif [ -e /etc/redhat-release ]
    then
        echo "The current OS is Fedora based"
        echo "--------Redhat Version--------"
        cat /etc/redhat-release
        echo "------------------------------"

        # use dnf on fedora
        # use yum on centos and redhat
        if [ -e /etc/fedora-release ]
        then
            command -v dnf
            if [ $? -eq 0 ]
            then
                useCompatSsl=0
                grep -i 'fedora release 27' /etc/fedora-release
                if [ $? -eq 0 ]
                then
                    useCompatSsl=1
                else
                    grep -i 'fedora release 26' /etc/fedora-release
                    if [ $? -eq 0 ]
                    then
                        useCompatSsl=1
                    fi
                fi

                if [ $useCompatSsl -eq 1 ]
                then
                    echo "Use compat-openssl10-devel instead of openssl-devel for Fedora 26/27 (dotnet core requires openssl 1.0.x)"                    
                    dnf install -y libunwind lttng-ust libcurl compat-openssl10 libuuid krb5-libs zlib libicu
                    if [ $? -ne 0 ]
                    then
                        echo "'dnf' failed with exit code '$?'"
                        print_errormessage
                        exit 1
                    fi
                else
                    dnf install -y libunwind lttng-ust libcurl openssl-libs libuuid krb5-libs zlib libicu
                    if [ $? -ne 0 ]
                    then
                        echo "'dnf' failed with exit code '$?'"
                        print_errormessage
                        exit 1
                    fi
                fi                
            else
                echo "Can not find 'dnf'"
                print_errormessage
                exit 1
            fi
        else
            command -v yum
            if [ $? -eq 0 ]
            then
                yum install -y libunwind libcurl openssl-libs libuuid krb5-libs zlib libicu
                if [ $? -ne 0 ]
                then                    
                    echo "'yum' failed with exit code '$?'"
                    print_errormessage
                    exit 1
                fi

                # install lttng-ust separately since it's not part of offical package repository
                yum install -y wget && wget -P /etc/yum.repos.d/ https://packages.efficios.com/repo.files/EfficiOS-RHEL7-x86-64.repo && rpmkeys --import https://packages.efficios.com/rhel/repo.key && yum updateinfo && yum install -y lttng-ust
                if [ $? -ne 0 ]
                then                    
                    echo "'lttng-ust' installation failed with exit code '$?'"
                    print_errormessage
                    exit 1
                fi
            else
                echo "Can not find 'yum'"
                print_errormessage
                exit 1
            fi
        fi
    else
        # we might on OpenSUSE
        OSTYPE=$(grep ID_LIKE /etc/os-release | cut -f2 -d=)
        echo $OSTYPE
        if [ $OSTYPE == '"suse"' ]
        then
            echo "The current OS is SUSE based"
            command -v zypper
            if [ $? -eq 0 ]
            then
                zypper -n install libunwind lttng-ust libcurl4 libopenssl1_0_0 libuuid1 krb5 zlib libicu52_1
                if [ $? -ne 0 ]
                then
                    echo "'zypper' failed with exit code '$?'"
                    print_errormessage
                    exit 1
                fi
            else
                echo "Can not find 'zypper'"
                print_errormessage
                exit 1
            fi
        else
            echo "Can't detect current OS type base on /etc/os-release."
            print_errormessage
            exit 1
        fi
    fi
else
    echo "/etc/os-release doesn't exists."
    print_errormessage
    exit 1
fi

echo "-----------------------------"
echo " Finish Install Dependencies"
echo "-----------------------------"