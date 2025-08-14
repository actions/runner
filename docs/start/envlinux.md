

# ![Linux](../res/linux_med.png) Linux System Prerequisites

## Supported Distributions and Versions

Please see "[Supported architectures and operating systems for self-hosted runners](https://docs.github.com/en/actions/reference/runners/self-hosted-runners#linux)."

## Quick Setup

The `./config.sh` script will automatically check and guide you through installing .NET dependencies:

```bash
./config.sh
# If dependencies are missing, run:
./bin/installdependencies.sh
```

## Install .NET Core Linux Dependencies

The `./config.sh` will check .NET Core dependencies during runner configuration.  
You might see something like this which indicates a dependency is missing:

```bash
./config.sh
    libunwind.so.8 => not found
    libunwind-x86_64.so.8 => not found
Dependencies is missing for Dotnet Core 6.0
Execute ./bin/installdependencies.sh to install any missing Dotnet Core 6.0 dependencies.
```

You can easily correct the problem by executing `./bin/installdependencies.sh`.  
The `installdependencies.sh` script should install all required dependencies on all supported Linux versions  

> **Note:** The `installdependencies.sh` script will try to use the default package management mechanism on your Linux flavor (ex. `yum`/`apt-get`/`apt`).

## Manual Dependency Installation

If the automatic installation doesn't work, you can manually install dependencies using your package manager:

### Debian based OS (Debian, Ubuntu, Linux Mint)

```bash
sudo apt-get update
sudo apt-get install -y liblttng-ust1 libkrb5-3 zlib1g libssl1.1 libicu66
```

**Required packages:**
- liblttng-ust1 or liblttng-ust0
- libkrb5-3
- zlib1g
- libssl1.1, libssl1.0.2 or libssl1.0.0
- libicu63, libicu60, libicu57 or libicu55

### Fedora based OS (Fedora, Red Hat Enterprise Linux, CentOS, Oracle Linux 7)

```bash
sudo yum install -y lttng-ust openssl-libs krb5-libs zlib libicu
# Or for newer systems:
sudo dnf install -y lttng-ust openssl-libs krb5-libs zlib libicu
```

**Required packages:**
- lttng-ust
- openssl-libs
- krb5-libs
- zlib
- libicu

### SUSE based OS (OpenSUSE, SUSE Enterprise)

```bash
sudo zypper install -y lttng-ust libopenssl1_1 krb5 zlib libicu60_2
```

**Required packages:**
- lttng-ust
- libopenssl1_1
- krb5
- zlib
- libicu60_2

## Troubleshooting

### Common Issues

**Permission denied errors:**
```bash
sudo chmod +x ./config.sh ./run.sh
```

**Missing dependencies after installation:**
```bash
# Check what's missing
ldd ./bin/Runner.Listener
# Reinstall dependencies
./bin/installdependencies.sh
```

**SSL/TLS errors:**
```bash
# Update certificates
sudo apt-get update && sudo apt-get install ca-certificates
# Or for RHEL/CentOS:
sudo yum update ca-certificates
```

### Getting Help

- Check our [troubleshooting guide](../checks/README.md)
- Search [GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions)
- Review [common network issues](../checks/network.md)

## [More .NET Core Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)
