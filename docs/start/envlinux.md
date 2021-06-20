

# ![Linux](../res/linux_med.png) Linux System Prerequisites

## Supported Distributions and Versions

x64
  - Red Hat Enterprise Linux 7
  - CentOS 7
  - Oracle Linux 7
  - Fedora 29+
  - Debian 9+
  - Ubuntu 16.04+
  - Linux Mint 18+
  - openSUSE 15+
  - SUSE Enterprise Linux (SLES) 12 SP2+

## Install .Net Core 3.x Linux Dependencies

The `./config.sh` will check .Net Core 3.x dependencies during runner configuration.  
You might see something like this which indicate a dependency's missing.
```bash
./config.sh
    libunwind.so.8 => not found
    libunwind-x86_64.so.8 => not found
Dependencies is missing for Dotnet Core 3.0
Execute ./bin/installdependencies.sh to install any missing Dotnet Core 3.0 dependencies.
```
You can easily correct the problem by executing `./bin/installdependencies.sh`.  
The `installdependencies.sh` script should install all required dependencies on all supported Linux versions   
> Note: The `installdependencies.sh` script will try to use the default package management mechanism on your Linux flavor (ex. `yum`/`apt-get`/`apt`).

### Full dependencies list

Debian based OS (Debian, Ubuntu, Linux Mint)

- liblttng-ust0
- libkrb5-3 
- zlib1g
- libssl1.1, libssl1.0.2 or libssl1.0.0
- libicu63, libicu60, libicu57 or libicu55

Fedora based OS (Fedora, Red Hat Enterprise Linux, CentOS, Oracle Linux 7)

- lttng-ust 
- openssl-libs 
- krb5-libs
- zlib
- libicu

SUSE based OS (OpenSUSE, SUSE Enterprise)

- lttng-ust
- libopenssl1_1
- krb5
- zlib
- libicu60_2

## [More .Net Core Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)
