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
copy "%~dp0run-helper.cmd.template" "%~dp0run-helper.cmd" /Y
call "%~dp0run-helper.cmd" %*
  
if %ERRORLEVEL% EQU 1 (
  echo "Restarting runner..."
  goto :launch_helper
)

if "%ACTIONS_RUNNER_RETURN_VERSION_DEPRECATED_EXIT_CODE%"=="1" (
  if %ERRORLEVEL% EQU 7 (
    echo "Exiting runner with deprecated version error code: %ERRORLEVEL%"
    exit /b %ERRORLEVEL%
  )
)

echo "Exiting runner..."
exit /b 0
