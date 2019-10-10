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

if /i "%~1" equ "localRun" (
    rem ********************************************************************************
    rem Local run.
    rem ********************************************************************************
    "%~dp0bin\Runner.Listener.exe" %*
) else (
  rem ********************************************************************************
  rem Run.
  rem ********************************************************************************
  "%~dp0bin\Runner.Listener.exe" run %*

  rem Return code 4 means the run once runner received an update message.
  rem Sleep 5 seconds to wait for the update process finish and run the runner again.
  if ERRORLEVEL 4 (
    timeout /t 5 /nobreak > NUL
    "%~dp0bin\Runner.Listener.exe" run %*
  )
)
