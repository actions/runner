@echo off

rem ********************************************************************************
rem Unblock specific files.
rem ********************************************************************************
setlocal
if defined VERBOSE_ARG (
  set VERBOSE_ARG='Continue'
) else (
  set VERBOSE_ARG='SilentlyContinue'
)

rem Unblock files in the root of the layout folder. E.g. .cmd files.
powershell.exe -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "$VerbosePreference = %VERBOSE_ARG% ; Get-ChildItem -LiteralPath '%~dp0' | ForEach-Object { Write-Verbose ('Unblock: {0}' -f $_.FullName) ; $_ } | Unblock-File | Out-Null"


rem ********************************************************************************
rem Run.
rem ********************************************************************************

:launch_helper
call "%~dp0bin\run-helper.cmd" %*

rem using `if %ERRORLEVEL% EQU N` insterad of `if ERRORLEVEL N`
rem `if ERRORLEVEL N` means: error level is N or MORE
  
if %ERRORLEVEL% EQU 1 (
  echo "Restart runner after it exited with return code '%ERRORLEVEL%'"
  goto :launch_helper
) else (  
  echo "Exit runner after it exited with return code '%ERRORLEVEL%'"
  exit 0
)
