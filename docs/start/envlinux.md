

# ![Linux](../res/linux_med.png) Linux System Prerequisites

## Supported Distributions and Versions

x64
  - Red Hat Enterprise Linux 6 (see note 1), 7
  - CentOS 6 (see note 1), 7
  - Oracle Linux 7
  - Fedora 28, 27
  - Debian 9, 8.7 or later versions
  - Ubuntu 18.04, Ubuntu 16.04, Ubuntu 14.04
  - Linux Mint 18, 17
  - openSUSE 42.3 or later versions
  - SUSE Enterprise Linux (SLES) 12 SP2 or later versions

ARM32 (see note 2)
  - Debian 9 or later versions
  - Ubuntu 18.04 or later versions

> Note 1: Red Hat Enterprise Linux 6 and CentOS 6 require installing the specialized "rhel.6-x64" agent package
> Note 2: ARM instruction set [ARMv7](https://en.wikipedia.org/wiki/List_of_ARM_microarchitectures) or above is required, you can get your device's information by executing `uname -a`

## Install .Net Core 2.x Linux Dependencies

The `./config.sh` will check .Net Core 2.x dependencies during agent configuration.  
You might see something like this which indicate a dependency's missing.
```bash
./config.sh
    libunwind.so.8 => not found
    libunwind-x86_64.so.8 => not found
Dependencies is missing for Dotnet Core 2.1
Execute ./bin/installdependencies.sh to install any missing Dotnet Core 2.1 dependencies.
```
You can easily correct the problem by executing `./bin/installdependencies.sh`.  
The `installdependencies.sh` script should install all required dependencies on all supported Linux versions   
> Note: The `installdependencies.sh` script will try to use the default package management mechanism on your Linux flavor (ex. `yum`/`apt-get`/`apt`). You might need to deal with error coming from the package management mechanism related to your setup, like [#1353](https://github.com/Microsoft/vsts-agent/issues/1353)

## [More .Net Core Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)
