@echo off

"%~dp0Runner.Listener.exe" run %*

rem using `if %ERRORLEVEL% EQU N` insterad of `if ERRORLEVEL N`
rem `if ERRORLEVEL N` means: error level is N or MORE
  
if %ERRORLEVEL% EQU 0 (
  echo "Runner listener exit with 0 return code, stop the service, no retry needed."
)

if %ERRORLEVEL% EQU 1 (
  echo "Runner listener exit with terminated error, stop the service, no retry needed."
)

if %ERRORLEVEL% EQU 2 (
  echo "Runner listener exit with retryable error, re-launch runner in 5 seconds."
  timeout /t 5 /nobreak > NUL
  goto :launch_listener
)

if %ERRORLEVEL% EQU 3 (
  echo "Runner listener exit because of updating, re-launch runner in 5 seconds"
  timeout /t 5 /nobreak > NUL
)

if %ERRORLEVEL% EQU 4 (
  echo "Runner listener exit because of updating, re-launch ephemeral runner in 5 seconds"
  timeout /t 5 /nobreak > NUL
)

if %ERRORLEVEL% GEQ 5 (  
  echo "Exiting with unknown error code: ${returnCode}"
)
if [%ERRORLEVEL%]==[] (  
  echo "Exiting with empty error code: ${returnCode}"
)

exit /b %ERRORLEVEL%