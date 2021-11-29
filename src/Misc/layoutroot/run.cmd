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

:relaunch_listener
"%~dp0bin\Runner.Listener.exe" run %*

rem using `if %ERRORLEVEL% EQU N` insterad of `if ERRORLEVEL N`
rem `if ERRORLEVEL N` means: error level is N or MORE
  
if %ERRORLEVEL% EQU 0 (
  echo "Runner listener exit with 0 return code, stop the service, no retry needed."
  exit %ERRORLEVEL%
)

if %ERRORLEVEL% EQU 1 (
  echo "Runner listener exit with terminated error, stop the service, no retry needed."
  exit %ERRORLEVEL%
)

if %ERRORLEVEL% EQU 2 (
  echo "Runner listener exit with retryable error, re-launch runner in 5 seconds."
  timeout /t 5 /nobreak > NUL
  goto :launch_listener
)

rem Return code 4 means the runner received an update message.
if %ERRORLEVEL% EQU 3 (
  echo "Runner listener exit because of updating, re-launch runner in 5 seconds"
  timeout /t 5 /nobreak > NUL
  goto :launch_listener
)

rem Return code 4 means the ephemeral runner received an update message.
if %ERRORLEVEL% EQU 4 (
  echo "Runner listener exit because of updating, re-launch ephemeral runner in 5 seconds"
  timeout /t 5 /nobreak > NUL
  goto :launch_listener
)
