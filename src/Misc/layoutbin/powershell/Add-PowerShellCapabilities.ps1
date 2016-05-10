[CmdletBinding()]
param()

Write-Capability -Name 'PowerShell' -Value $PSVersionTable.PSVersion
