# Install .NET Core

.NET Core version 1.0.0.001598 must be used for Windows, OSX and version 1.0.0.001598-1 is needed for Ubuntu.
Installation instructions are available [here](https://dotnet.github.io/getting-started/) Please use the links below to download the correct version of Windows/OSX installers.


## [.Net Core for Windows](https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0.001598/dotnet-win-x64.1.0.0.001598.exe)

Uninstall any existing version first (Programs and Features -> Microsoft Dotnet CLI for Windows).

## [.Net Core for OSX 10.11](https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/1.0.0.001598/dotnet-osx-x64.1.0.0.001598.pkg)


## .Net Core for Ubuntu 14.04

There is no installer for Ubuntu, but instead there are 4 commands to run:

sudo sh -c 'echo "deb [arch=amd64] http://apt-mo.trafficmanager.net/repos/dotnet/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'

sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893

sudo apt-get update

sudo apt-get install dotnet=1.0.0.001598-1

