#!/bin/bash

user_id=`id -u`

if [ $user_id -ne 0 ]; then
    echo "Need to run with sudo privilege"
    exit 1
fi

# Determine OS type 
# Debian based OS (Debian, Ubuntu, Linux Mint) has /etc/debian_version
# Fedora based OS (Fedora, Red Hat Enterprise Linux, CentOS, Oracle Linux 7) has /etc/redhat-release
# SUSE based OS (OpenSUSE, SUSE Enterprise) has ID_LIKE=suse in /etc/os-release

function print_errormessage() 
{
    echo "Can't install dotnet core dependencies."
    echo "You can manually install all required dependencies based on following documentation"
    echo "https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x"
}

function print_rhel6message() 
{
    echo "We did our best effort to install dotnet core dependencies"
    echo "However, there are some dependencies which require manual installation" 
    echo "You can install all remaining required dependencies based on the following documentation"
    echo "https://github.com/dotnet/core/blob/master/Documentation/build-and-install-rhel6-prerequisites.md"
}

function print_rhel6errormessage() 
{
    echo "We couldn't install dotnet core dependencies"
    echo "You can manually install all required dependencies based on following documentation"
    echo "https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x"
    echo "In addition, there are some dependencies which require manual installation. Please follow this documentation" 
    echo "https://github.com/dotnet/core/blob/master/Documentation/build-and-install-rhel6-prerequisites.md"
}

function install_with_fallbacks() {
    local install_cmd="$1"
    shift
    local packages=("$@")
    
    for package in "${packages[@]}"; do
        echo "Trying to install: $package"
        if eval "$install_cmd $package"; then
            echo "Successfully installed: $package"
            return 0
        else
            echo "Failed to install: $package, trying next option..."
        fi
    done
    
    echo "All installation attempts failed for package alternatives: ${packages[*]}"
    return 1
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
        
        # prefer apt-get over apt
        if command -v apt-get > /dev/null 2>&1; then
            apt_get="apt-get"
        elif command -v apt > /dev/null 2>&1; then
            apt_get="apt"
        else
            echo "Found neither 'apt-get' nor 'apt'"
            print_errormessage
            exit 1
        fi

        # Install basic dependencies
        $apt_get update && $apt_get install -y libkrb5-3 zlib1g
        if [ $? -ne 0 ]; then
            echo "'$apt_get' failed"
            print_errormessage
            exit 1
        fi

        # Install lttng with fallbacks
        if ! install_with_fallbacks "$apt_get install -y" "liblttng-ust1" "liblttng-ust0"; then
            print_errormessage
            exit 1
        fi

        # Install SSL with fallbacks  
        if ! install_with_fallbacks "$apt_get install -y" "libssl1.1" "libssl1.0.2" "libssl1.0.0"; then
            print_errormessage
            exit 1
        fi

        # Install ICU with fallbacks
        if ! install_with_fallbacks "$apt_get install -y" "libicu72" "libicu71" "libicu70" "libicu69" "libicu68" "libicu67" "libicu66" "libicu65" "libicu63" "libicu60" "libicu57" "libicu55" "libicu52"; then
            print_errormessage
            exit 1
        fi

    elif [ -e /etc/redhat-release ]
    then
        echo "The current OS is Fedora based"
        echo "--Fedora/RHEL/CentOS Version--"
        cat /etc/redhat-release
        echo "------------------------------"

        if [ -e /etc/fedora-release ]; then
            # Fedora - use dnf
            if ! command -v dnf > /dev/null 2>&1; then
                echo "Can not find 'dnf'"
                print_errormessage
                exit 1
            fi

            # Install lttng with fallbacks
            if ! install_with_fallbacks "dnf install -y" "lttng-ust1" "lttng-ust"; then
                print_errormessage
                exit 1
            fi

            # Install other dependencies
            dnf install -y openssl-libs krb5-libs zlib libicu
            if [ $? -ne 0 ]; then
                echo "'dnf' failed"
                print_errormessage
                exit 1
            fi
        else
            # RHEL/CentOS - use yum
            if ! command -v yum > /dev/null 2>&1; then
                echo "Can not find 'yum'"
                print_errormessage
                exit 1
            fi

            # Install lttng with fallbacks
            if ! install_with_fallbacks "yum install -y" "lttng-ust1" "lttng-ust"; then
                print_errormessage
                exit 1
            fi

            # Install other dependencies
            yum install -y openssl-libs krb5-libs zlib libicu
            if [ $? -ne 0 ]; then
                echo "'yum' failed"
                print_errormessage
                exit 1
            fi
        fi
    else
        # we might be on OpenSUSE
        OSTYPE=$(grep ID_LIKE /etc/os-release | cut -f2 -d=)
        echo $OSTYPE
        if echo $OSTYPE | grep -q "suse"; then
            echo "The current OS is SUSE based"
            
            if ! command -v zypper > /dev/null 2>&1; then
                echo "Can not find 'zypper'"
                print_errormessage
                exit 1
            fi

            # Install lttng with fallbacks
            if ! install_with_fallbacks "zypper -n install" "lttng-ust1" "lttng-ust"; then
                print_errormessage
                exit 1
            fi

            # Install other dependencies
            zypper -n install libopenssl1_1 krb5 zlib libicu60_2
            if [ $? -ne 0 ]; then
                echo "'zypper' failed"
                print_errormessage
                exit 1
            fi
        else
            echo "Can't detect current OS type based on /etc/os-release."
            print_errormessage
            exit 1
        fi
    fi
elif [ -e /etc/redhat-release ]
# RHEL6 doesn't have an os-release file defined, read redhat-release instead
then
    redhatRelease=$(</etc/redhat-release)
    if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]
    then
        echo "The current OS is Red Hat Enterprise Linux 6 or CentOS 6"

        # Install known dependencies, as a best effort.
        # The remaining dependencies are covered by the GitHub doc that will be shown by `print_rhel6message`
        command -v yum
        if [ $? -eq 0 ]
        then
            yum install -y openssl krb5-libs zlib
            if [ $? -ne 0 ]
            then                    
                echo "'yum' failed with exit code '$?'"
                print_rhel6errormessage
                exit 1
            fi
        else
            echo "Can not find 'yum'"
            print_rhel6errormessage
            exit 1
        fi

        print_rhel6message
        exit 1
    else
        echo "Unknown RHEL OS version"
        print_errormessage
        exit 1
    fi
else
    echo "Unknown OS version"
    print_errormessage
    exit 1
fi

echo "-----------------------------"
echo " Finish Install Dependencies"
echo "-----------------------------"
