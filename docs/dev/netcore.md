# Install .NET Core

Building and packaging the agent, MUST be done with a specific version of the toolchain and .NET Core.  They are changing rapidly and our build process assumes this version.  We are looking into the env pulling that version.

To build, we rely on .NET Core SDK version 1.0.0-preview2-002875

Additional instructions are available [here](https://www.microsoft.com/net/core#windows) Please use the links below to download the correct version of Windows/OSX SDK files.


## ![Win](../res/win_med.png) Windows

Download the archive, extract and add the new folder to the PATH [dotnet-dev-win-x64.1.0.0-preview2-002875.zip](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/1.0.0-preview2-002875/dotnet-dev-win-x64.1.0.0-preview2-002875.zip)

> <sub><sup>TIP: Uninstall any existing version first (Programs and Features -> Microsoft Dotnet CLI for Windows).</sup></sub>  

## ![Apple](../res/apple_med.png) OSX 10.11  

Download the archive, extract and add the new folder to the PATH [dotnet-dev-osx-x64.1.0.0-preview2-002875.tar.gz](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/1.0.0-preview2-002875/dotnet-dev-osx-x64.1.0.0-preview2-002875.tar.gz)

> <sub><sup>TIP: Uninstall any existing version first by deleting the folder that contains "dotnet" command.</sup></sub>  


## ![Linux](../res/linux_med.png) Ubuntu 14.04

The script below downloads and extracts .Net Core in a folder ~/dotnet. Please add this folder to your PATH.  

```bash
mkdir ~/dotnet  

cd ~/dotnet  

wget https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/1.0.0-preview2-002875/dotnet-dev-ubuntu-x64.1.0.0-preview2-002875.tar.gz

tar zxfv dotnet-dev-ubuntu-x64.1.0.0-preview2-002875.tar.gz
```

> <sub><sup>TIP: Uninstall any existing version first by listing existing packages containing "dotnet" in the name with "dpkg --get-selections | grep dotnet" and then uninstall one by one with "sudo apt-get purge dotnet_package_name".</sup></sub>

## ![Redhat](../res/redhat_med.png) Red Hat Enterprise Linux 7.2

The script below downloads and extracts .Net Core in a folder ~/dotnet. Please add this folder to your PATH (usually by editing ~/.bash_profile).  We install several yum packages needed by .Net Core and our dev.sh script.

```bash
mkdir ~/dotnet  

cd ~/dotnet  

wget https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/1.0.0-preview2-002875/dotnet-dev-rhel-x64.1.0.0-preview2-002875.tar.gz  

tar zxfv dotnet-dev-rhel-x64.1.0.0-preview2-002875.tar.gz

sudo yum -y install libunwind.x86_64 icu git curl zip unzip
```

