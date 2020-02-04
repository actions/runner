@setlocal
@echo off
 rem add expected utils to path
IF EXIST C:\Program Files\Git\usr\bin (
  SET PATH=C:\Program Files\Git\usr\bin;%PATH%
)
IF EXIST C:\Program Files\Git\mingw64\bin (
  SET PATH=C:\Program Files\Git\mingw64\bin;%PATH%
)
IF EXIST C:\Program Files\Git\bin (
  SET PATH=C:\Program Files\Git\bin;%PATH%
)

 rem Check if SH_PATH is defined.
if defined SH_PATH (
  goto run
)

 rem Attempt to resolve sh.exe from the PATH.
where sh.exe 1>"%TEMP%\where_sh" 2>nul
set /p SH_PATH= < "%TEMP%\where_sh"
del "%TEMP%\where_sh"
if defined SH_PATH (
  goto run
)

 rem Check well-known locations.
set SH_PATH=C:\Program Files\Git\bin\sh.exe
if exist "%SH_PATH%" (
  goto run
)

 rem Check well-known locations.
set SH_PATH=%LOCALAPPDATA%\Programs\Git\bin\sh.exe
if exist "%SH_PATH%" (
  goto run
)

echo Unable to resolve location of sh.exe. 1>&2
exit /b 1

:run
echo on
"%SH_PATH%" "%~dp0dev.sh" %*
