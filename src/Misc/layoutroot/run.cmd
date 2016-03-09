@setlocal
@echo off

REM TODO: unblock files

:run
%~dp0\bin\Agent.Listener.exe %*

