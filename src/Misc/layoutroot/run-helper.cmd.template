@echo off
SET UPDATEFILE=update.finished
"%~dp0\bin\Runner.Listener.exe" run %*

rem using `if %ERRORLEVEL% EQU N` instead of `if ERRORLEVEL N`
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
  rem Wait for 30 seconds or for flag file to exists for the ephemeral runner update process finish
  echo "Runner listener exit because of updating, re-launch runner after successful update"
  FOR /L %%G IN (1,1,30) DO (
    IF EXIST %UPDATEFILE% (
      echo "Update finished successfully."
      del %FILE%
      exit /b 1
    )
    ping 127.0.0.1 -n 2 -w 1000 >NUL
  )
  exit /b 1
)

if %ERRORLEVEL% EQU 4 (
  rem Wait for 30 seconds or for flag file to exists for the runner update process finish
  echo "Runner listener exit because of updating, re-launch runner after successful update"
  FOR /L %%G IN (1,1,30) DO (
    IF EXIST %UPDATEFILE% (
      echo "Update finished successfully."
      del %FILE%
      exit /b 1
    )
    ping 127.0.0.1 -n 2 -w 1000 >NUL
  )
  exit /b 1
)

echo "Exiting after unknown error code: %ERRORLEVEL%"
exit /b 0