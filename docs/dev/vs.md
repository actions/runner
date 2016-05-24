# Using Visual Studio 2015  

Install Visual Studio 2015 update 1 or later

## Create Symbolic link
1. Remove packages folder from %USERPROFILE%\.dnx\packages
2. Run cmd.exe as Administrator
3. Execute this command => `mklink /D %USERPROFILE%\.dnx\packages %USERPROFILE%\.nuget\packages`

## Package restore
Restore the packages using command, because VS won't be able to do it.

1. Navigate to src folder from the command prompt
2. Run => `dev r`
  
## VS2015
1. Open File->Open->Project/Solution
2. Select project.json file that you want to debug

Visual Studio will create a solution and "xproj" project files. These files should not be under source control.

Press F5 to debug
