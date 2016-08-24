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

rem Unblock the following types of files:
rem 1) The files in the root of the layout folder. E.g. .cmd files.
rem
rem 2) The PowerShell scripts delivered with the agent. E.g. capability scan scripts under "bin\"
rem and legacy handler scripts under "externals\vstshost\".
rem
rem 3) The DLLs potentially loaded from a PowerShell script (e.g. DLLs in Agent.ServerOMDirectory).
rem Otherwise, Add-Type may result in the following error:
rem   Add-Type : Could not load file or assembly 'file:///[...].dll' or one of its dependencies.
rem   Operation is not supported.
rem Reproduced on Windows 8 in PowerShell 4. Changing the execution policy did not appear to make
rem a difference. The error reproduced even with the execution policy set to Bypass. It may be a
rem a policy setting.
powershell.exe -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "$VerbosePreference = %VERBOSE_ARG% ; Get-ChildItem -LiteralPath '%~dp0' | ForEach-Object { Write-Verbose ('Unblock: {0}' -f $_.FullName) ; $_ } | Unblock-File | Out-Null ; Get-ChildItem -Recurse -LiteralPath '%~dp0bin', '%~dp0externals' | Where-Object { $_ -match '\.(ps1|psd1|psm1)$' } | ForEach-Object { Write-Verbose ('Unblock: {0}' -f $_.FullName) ; $_ } | Unblock-File | Out-Null ; Get-ChildItem -LiteralPath '%~dp0externals\vstsom', '%~dp0externals\vstshost' | Where-Object { $_ -match '\.(dll|exe)$' } | ForEach-Object { Write-Verbose ('Unblock: {0}' -f $_.FullName) ; $_ } | Unblock-File | Out-Null"

if "%~1" equ "remove" (
    rem ********************************************************************************
    rem Unconfigure the agent.
    rem ********************************************************************************
    "%~dp0bin\Agent.Listener.exe" %*
) else (
    rem ********************************************************************************
    rem Configure the agent.
    rem ********************************************************************************
    "%~dp0bin\Agent.Listener.exe" configure %*
)
