

# ![Linux](../res/linux_med.png) Linux System Prerequisites

## Supported Distributions and Versions

Please see "[Supported architectures and operating systems for self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners#linux)."

## Install .NET 6.x Linux Dependencies

The `./config.sh` will check .NET 6.x dependencies during runner configuration.  
You might see something like this which indicate a dependency's missing.
```bash
./config.sh
    libunwind.so.8 => not found
    libunwind-x86_64.so.8 => not found
Dependencies are missing for .NET 6.0
Execute ./bin/installdependencies.sh to install any missing .NET 6.0 dependencies.
```
You can easily correct the problem by executing `./bin/installdependencies.sh`.  
The `installdependencies.sh` script should install all required dependencies on all supported Linux versions  
> Note: The `installdependencies.sh` script will try to use the default package management mechanism on your Linux flavor (ex. `yum`/`apt-get`/`apt`).

### Full dependencies list

Debian based OS (Debian, Ubuntu, Linux Mint)

- liblttng-ust1 or liblttng-ust0
- libkrb5-3
- zlib1g
- libssl1.1, libssl1.0.2 or libssl1.0.0
- libicu75, libicu74, libicu73, libicu72, libicu71, libicu70, libicu69, libicu68, libicu67, libicu66, libicu65, libicu63, libicu60, libicu57, libicu55, libicu52

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
