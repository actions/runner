# Install .NET Core

Building and packaging the agent, MUST be done with a specific version of the toolchain and .NET Core.  They are changing rapidly and our build process assumes this version.  We are looking into the env pulling that version.

To build, we rely on .NET Core SDK version 1.0.0-rc2-002416 

Additional instructions are available [here](https://dotnet.github.io/getting-started/) Please use the links below to download the correct version of Windows/OSX installers.


## ![Win](../win_med.png) Windows

Install [dotnet-dev-win-x64.1.0.0-rc2-002416.exe](https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0-rc2-002416/dotnet-dev-win-x64.1.0.0-rc2-002416.exe)

> <sub><sup>TIP: Uninstall any existing version first (Programs and Features -> Microsoft Dotnet CLI for Windows).</sup></sub>  

## ![Apple](../apple_med.png) OSX 10.11  

Install [dotnet-dev-osx-x64.1.0.0-rc2-002416.pkg](https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0-rc2-002416/dotnet-dev-osx-x64.1.0.0-rc2-002416.pkg)

> <sub><sup>TIP: Uninstall any existing version first by deleting the folder that contains "dotnet" command.</sup></sub>  


## ![Linux](../linux_med.png) Ubuntu 14.04

On Ubuntu, it must be installed using dpkg  

```bash
mkdir ~/dotnetpackages  

wget -P ~/dotnetpackages https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0-rc2-002416/dotnet-host-ubuntu-x64.1.0.0-rc2-002416.deb  

wget -P ~/dotnetpackages https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0-rc2-3002416/dotnet-sharedframework-ubuntu-x64.1.0.0-rc2-3002416.deb  

wget -P ~/dotnetpackages https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0-rc2-002416/dotnet-sdk-ubuntu-x64.1.0.0-rc2-002416.deb

sudo dpkg -i ~/dotnetpackages/dotnet-host-ubuntu-x64.1.0.0-rc2-002416.deb

sudo dpkg -i ~/dotnetpackages/dotnet-sharedframework-ubuntu-x64.1.0.0-rc2-3002416.deb

sudo dpkg -i ~/dotnetpackages/dotnet-sdk-ubuntu-x64.1.0.0-rc2-002416.deb
```

> <sub><sup>TIP: Uninstall any existing version first by listing existing packages containing "dotnet" in the name with "dpkg --get-selections | grep dotnet" and then uninstall one by one with "sudo apt-get purge dotnet_package_name".</sup></sub>