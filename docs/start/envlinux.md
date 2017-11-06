

# ![Linux](../res/linux_med.png) Linux System Prerequisites [2.125.0 or above]

## Supported Distributions and Versions
  - Red Hat Enterprise Linux 7
  - CentOS 7
  - Oracle Linux 7
  - Fedora 25, Fedora 26
  - Debian 8.7 or later versions
  - Ubuntu 17.04, Ubuntu 16.04, Ubuntu 14.04
  - Linux Mint 18, Linux Mint 17
  - openSUSE 42.2 or later versions
  - SUSE Enterprise Linux (SLES) 12 SP2 or later versions

## Install .Net Core 2.x Linux Dependencies

The `./config.sh` will check .Net Core 2.x dependnecies during agent configuration.  
You might see something like this which indicate a dependencies missing.
```bash
./config.sh
    libunwind.so.8 => not found
    libunwind-x86_64.so.8 => not found
Dependencies is missing for Dotnet Core 2.0
Execute ./bin/installdependencies.sh to install any missing Dotnet Core 2.0 dependencies.
```
You can easily correct the problem by execute `./bin/installdependencies.sh`.  
The `installdependencies.sh` script should install all required dependencies on all supported Linux versions   


## Git

If you use git, git >= 2.9.0 is a pre-requisite for Linux agents.

## Optionally Java if Tfvc

The agent distributes team explorer everywhere.

But, if you are using Tfvc, install Oracle Java 1.6+ as TEE uses Java.

## [More .Net Core Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)
