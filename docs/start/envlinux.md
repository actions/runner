

# ![Linux](../res/linux_med.png) Linux System Prerequisites

## Supported Distributions and Versions

Please see "[Supported architectures and operating systems for self-hosted runners](https://docs.github.com/en/actions/reference/runners/self-hosted-runners#linux)."

## Install .Net Core 3.x Linux Dependencies

The [config.sh](../../src/Misc/layoutroot/config.sh) will check .Net Core 3.x dependencies during runner configuration.  
You might see something like this which indicate a dependency's missing.
```bash
./config.sh
    libunwind.so.8 => not found
    libunwind-x86_64.so.8 => not found
Dependencies is missing for Dotnet Core 6.0
Execute ./bin/installdependencies.sh to install any missing Dotnet Core 6.0 dependencies.
```
You can easily correct the problem by executing [installdependencies.sh](../../src/Misc/layoutbin/installdependencies.sh).  
The `installdependencies.sh` script should install all required dependencies on all supported Linux versions  
> Note: The `installdependencies.sh` script will try to use the default package management mechanism on your Linux flavor (ex. `yum`/`apt-get`/`apt`).

### Full dependencies list

Debian based OS (Debian, Ubuntu, Linux Mint)

- liblttng-ust1t64, liblttng-ust1 or liblttng-ust0
- libkrb5-3
- zlib1g
- libssl3t64, libssl3, libssl1.1, libssl1.0.2 or libssl1.0.0
- libicu80, libicu79, ..., libicu66, libicu65, libicu63, libicu60, libicu57, libicu55, or libicu52
- libatomic1 (see [Node.js dependencies](#nodejs-dependencies))

Fedora based OS (Fedora, Red Hat Enterprise Linux, CentOS, Oracle Linux 7)

- lttng-ust
- openssl-libs
- krb5-libs
- zlib
- libicu
- libatomic (see [Node.js dependencies](#nodejs-dependencies))

SUSE based OS (OpenSUSE, SUSE Enterprise)

- lttng-ust
- libopenssl1_1
- krb5
- zlib
- libicu60_2
- libatomic1 (see [Node.js dependencies](#nodejs-dependencies))

## Node.js dependencies

The runner ships its own Node.js under `<runner_root>/externals/`, and workflows commonly install additional
Node.js versions into the tool cache with [actions/setup-node](https://github.com/actions/setup-node).

The official Node.js binaries link against `libatomic.so.1` starting with **Node.js 25**. If that library is
missing, Node.js exits before running anything:

```
node: error while loading shared libraries: libatomic.so.1: cannot open shared object file: No such file or directory
```

`installdependencies.sh` installs this library on a best-effort basis (`libatomic1` on Debian/SUSE based
distributions, `libatomic` on Fedora based ones). It is treated as best effort rather than a hard requirement
because the runner itself starts fine without it — only Node.js 25+ needs it — so a distribution that does not
package it still configures successfully, with a warning.

## [More .Net Core Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)
