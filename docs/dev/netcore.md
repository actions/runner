# Install .NET Core

Building and packaging the agent, MUST be done with a specific version of the toolchain and .NET Core.  They are changing rapidly and our build process assumes this version.  We are looking into the env pulling that version.

To build, we rely on .NET Core version 1.0.0.001598 

Additional instructions are available [here](https://dotnet.github.io/getting-started/) Please use the links below to download the correct version of Windows/OSX installers.


## ![Win](../win_med.png) Windows

Install [dotnet-win-x64.1.0.0.001598.exe](https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0.001598/dotnet-win-x64.1.0.0.001598.exe)

> <sub><sup>TIP: Uninstall any existing version first (Programs and Features -> Microsoft Dotnet CLI for Windows).</sup></sub>  

## ![Apple](../apple_med.png) OSX 10.11  

Install [dotnet-osx-x64.1.0.0.001598.pkg](https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0.001598/dotnet-osx-x64.1.0.0.001598.pkg)


## ![Linux](../linux_med.png) Ubuntu 14.04

On Ubuntu, it must be installed using apt-get  

```bash
sudo sh -c 'echo "deb [arch=amd64] http://apt-mo.trafficmanager.net/repos/dotnet/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'  

sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893  

sudo apt-get update  

sudo apt-get install dotnet=1.0.0.001598-1  
```

