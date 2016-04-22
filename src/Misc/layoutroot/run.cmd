@setlocal
@echo off

REM TODO: unblock files

:run
"%~dp0bin\Agent.Listener.exe" %*

