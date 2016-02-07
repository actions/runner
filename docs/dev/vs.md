# Using Visual Studio 2015  

Install Visual Studio 2015 update 1 or later

Replace %USERPROFILE%\.dnx\packages folder with a symbolic link to %USERPROFILE%\.nuget\packages
First delete %USERPROFILE%\.dnx\packages, then run cmd.exe as Administrator and execute the following command:
mklink /D %USERPROFILE%\.dnx\packages %USERPROFILE%\.nuget\packages

restore the packages using the commands documented above, because VS won't be able to do it

Start VS2015, File->Open->Project/Solution, then select project.json file that you want to debug
Visual Studio will create a solution and "xproj" project files. These files should not be under source control.
Press F5 to debug
