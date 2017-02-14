[CmdletBinding()]
param()

@(Get-Process -Name 'WindowsAzureGuestAgent' -ErrorAction Ignore) | Select-Object -First 1 | ForEach-Object { Write-Capability -Name 'AzureGuestAgent' -Value $_.Path }