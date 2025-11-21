#!/bin/bash

set -e -o pipefail

if [ "$(id -u)" -ne "0" ]; then
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

if [ -e /etc/os-release ]; then
    echo "--------OS Information--------"
    cat /etc/os-release
    echo "------------------------------"

    if [ -e /etc/debian_version ]; then
        echo "The current OS is Debian based"
        echo "--------Debian Version--------"
        cat /etc/debian_version
        echo "------------------------------"

        # prefer apt-get over apt
        if command -v apt-get; then
            apt_get=apt-get
        else
            if command -v apt; then
                apt_get=apt
            else
                echo "Found neither 'apt-get' nor 'apt'"
                print_errormessage
                exit 1
            fi
        fi

        if ! "$apt_get" update && "$apt_get" install -y libkrb5-3 zlib1g; then
            echo "'$apt_get' failed with exit code '$?'"
            print_errormessage
            exit 1
        fi

        apt_get_with_fallbacks() {
            fail=0
            if "$apt_get" install -y "$1"; then
                if ! dpkg -l | grep "^ii\s\+$1\s" &>/dev/null; then
                    fail=1
                fi
            else
                shift
                if [ $# -eq 0 ]; then
                    fail=1
                    return $fail
                else
                    apt_get_with_fallbacks "$@"
                fi
            fi
        }

        if ! apt_get_with_fallbacks liblttng-ust1 liblttng-ust0; then
            print_errormessage
            exit 1
        fi

        if ! apt_get_with_fallbacks libssl3 libssl1.1 libssl1.0.2 libssl1.0.0; then
            print_errormessage
            exit 1
        fi

        if ! apt_get_with_fallbacks libicu76 libicu75 libicu74 libicu73 libicu72 libicu71 libicu70 libicu69 libicu68 libicu67 libicu66 libicu65 libicu63 libicu60 libicu57 libicu55 libicu52; then
            print_errormessage
            exit 1
        fi
    elif [ -e /etc/redhat-release ]; then
        echo "--Fedora/RHEL/CentOS Version--"
        cat /etc/redhat-release
        echo "------------------------------"

        if command -v dnf; then
            dnf=dnf
        elif command -v yum; then
            dnf=yum
        else
            if command -v microdnf; then
                dnf=microdnf
            else
                echo "Found neither 'dnf', 'yum', nor 'microdnf'"
                print_errormessage
                exit 1
            fi
        fi

        if ! $dnf install -y lttng-ust openssl-libs krb5-libs zlib libicu; then
            exit_code=$?
            echo "'$dnf' failed with exit code '$exit_code'"
            print_errormessage
            exit 1
        fi
    else
        # we might on OpenSUSE
        OSTYPE=$(grep ID_LIKE /etc/os-release | cut -f2 -d=)
        echo "$OSTYPE"
        if echo "$OSTYPE" | grep "suse"; then
            echo "The current OS is SUSE based"
            if command -v zypper; then
                if ! zypper -n install lttng-ust libopenssl1_1 krb5 zlib libicu60_2; then
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
        if command -v yum; then
            if ! yum install -y openssl krb5-libs zlib; then
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
