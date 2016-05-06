[CmdletBinding()]
param()

Import-Module -Name 'Microsoft.PowerShell.Management'
Import-Module -Name 'Microsoft.PowerShell.Utility'
$ErrorActionPreference = 'Stop'
Import-Module -Name $PSScriptRoot\CapabilityHelpers

# Run each capability script.
foreach ($item in (Get-ChildItem -LiteralPath "$PSScriptRoot" -Filter "Add-*Capabilities.ps1")) {
    if ($item.Name -eq ([System.IO.Path]::GetFileName($PSCommandPath))) {
        continue;
    }

    Write-Host "& $($item.FullName)"
    try {
        & $item.FullName
    } catch {
        Write-Host ($_ | Out-String)
    }
}
