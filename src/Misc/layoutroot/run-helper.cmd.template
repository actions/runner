@echo off

"%~dp0\bin\Runner.Listener.exe" run %*

rem using `if %ERRORLEVEL% EQU N` insterad of `if ERRORLEVEL N`
rem `if ERRORLEVEL N` means: error level is N or MORE
  
if %ERRORLEVEL% EQU 0 (
  echo "Runner listener exit with 0 return code, stop the service, no retry needed."
  exit /b 0
)

if %ERRORLEVEL% EQU 1 (
  echo "Runner listener exit with terminated error, stop the service, no retry needed."
  exit /b 0
)

if %ERRORLEVEL% EQU 2 (
  echo "Runner listener exit with retryable error, re-launch runner in 5 seconds."
  ping 127.0.0.1 -n 6 -w 1000 >NUL
  exit /b 1
)

if %ERRORLEVEL% EQU 3 (
  rem Sleep 5 seconds to wait for the runner update process finish
  echo "Runner listener exit because of updating, re-launch runner in 5 seconds"
  ping 127.0.0.1 -n 6 -w 1000 >NUL
  exit /b 1
)

if %ERRORLEVEL% EQU 4 (
  rem Sleep 5 seconds to wait for the ephemeral runner update process finish
  echo "Runner listener exit because of updating, re-launch ephemeral runner in 5 seconds"
  ping 127.0.0.1 -n 6 -w 1000 >NUL
  exit /b 1
)

echo "Exiting after unknown error code: %ERRORLEVEL%"
exit /b 0