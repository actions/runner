# VSTS Cross Platform Agent (CoreCLR)

## Overview

A cross platform build and release agent for Visual Studio Team Services and Team Foundation Server 2015 and beyond.

Supported on Windows, OSX and Linux.  Written for the .NET Core CLR as one code base in C#.

## Install

Not available yet.  Need to build from source.  See Contribute below.

## Contribute (Dev)

### Dev Dependencies

![Win](docs/win_sm.png)![*nix](docs/linux_sm.png) .NET Core [Install Here](https://dotnet.github.io/getting-started/)  

![Win](docs/win_sm.png) Git for Windows [Install Here](https://git-scm.com/downloads) _(needed for dev sh script)

### Build, Test, Clean, Restore 

From src:

![Win](docs/win_sm.png) `dev {command}`  

![*nix](docs/linux_sm.png) `./dev.sh {command}`
  
** Commands: **

`restore` (`r`): Run first time and any time you change a project.json  

`build` (`b`):   build everything  

`test` (`t`):    run unit tests
        
  results in: 
  Test/bin/Debug/dnxcore50/{platform}/testResults.xml

`buildtest` (`bt`): build and test

`clean` (`c`):   deletes build output for each projects
 
`layout` (`l`): Creates a full layout in {root}/_layout  
   Does a clean, restore, build, publish and copy
  

# How to use Visual Studio 2015 for debugging  

Install Visual Studio 2015 update 1 or later

Replace %USERPROFILE%\.dnx\packages folder with a symbolic link to %USERPROFILE%\.nuget\packages
First delete %USERPROFILE%\.dnx\packages, then run cmd.exe as Administrator and execute the following command:
mklink /D %USERPROFILE%\.dnx\packages %USERPROFILE%\.nuget\packages

restore the packages using the commands documented above, because VS won't be able to do it

Start VS2015, File->Open->Project/Solution, then select project.json file that you want to debug
Visual Studio will create a solution and "xproj" project files. These files should not be under source control.
Press F5 to debug
