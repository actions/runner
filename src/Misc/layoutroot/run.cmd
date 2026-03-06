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
set RETURNCODE=%ERRORLEVEL%
set OUTDATED_EXIT_CODE_NUM=

if not "%ACTIONS_RUNNER_OUTDATED_EXIT_CODE%"=="" (
  set /a OUTDATED_EXIT_CODE_NUM=%ACTIONS_RUNNER_OUTDATED_EXIT_CODE% >NUL 2>NUL
  if errorlevel 1 set OUTDATED_EXIT_CODE_NUM=
)

if not "%OUTDATED_EXIT_CODE_NUM%"=="" (
  if %OUTDATED_EXIT_CODE_NUM% LEQ 5 set OUTDATED_EXIT_CODE_NUM=
)
  
if %RETURNCODE% EQU 1 (
  echo "Restarting runner..."
  goto :launch_helper
)

if not "%OUTDATED_EXIT_CODE_NUM%"=="" (
  if "%RETURNCODE%"=="%OUTDATED_EXIT_CODE_NUM%" (
    echo "Exiting runner with outdated error code: %RETURNCODE%"
    exit /b %RETURNCODE%
  )
)

echo "Exiting runner..."
exit /b 0
