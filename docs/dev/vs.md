# Using Visual Studio 2015  

Install Visual Studio 2015 update 1 or later

## Create Symbolic link
1. Remove packages folder: %USERPROFILE%\\.dnx\packages
2. Run cmd.exe as Administrator
3. Execute this command → `mklink /D %USERPROFILE%\.dnx\packages %USERPROFILE%\.nuget\packages`

## Build once from cmdline
Run the layout script once (restores packages, downloads externals, generates source files, etc). Building from VS won't do these steps.

1. Navigate to the `src` folder from the command prompt
2. Run => `dev layout`
  
## VS2015
1. Open File->Open->Project/Solution
2. Select project.json file that you want to debug

Visual Studio will create a solution and ".xproj" project files. These files should not be under source control.  They are currently set to be ignored by the repo's .gitignore file.

Press F5 to debug
