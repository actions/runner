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

if /i "%~1" equ "remove" (
    rem ********************************************************************************
    rem Unconfigure the runner.
    rem ********************************************************************************
    "%~dp0bin\Runner.Listener.exe" %*
) else (
    rem ********************************************************************************
    rem Configure the runner.
    rem ********************************************************************************
    "%~dp0bin\Runner.Listener.exe" configure %*
)
