[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Import the helper functions.
. $PSScriptRoot\CapabilityFunctions
. $PSScriptRoot\PathFunctions
. $PSScriptRoot\RegistryFunctions

# Export the public functions.
Export-ModuleMember -Function @(
    # Capability functions.
    'Add-CapabilityFromApplication'
    'Add-CapabilityFromEnvironment'
    'Add-CapabilityFromRegistry'
    'Write-Capability'
    # File system helpers.
    'Test-Container'
    'Test-Leaf'
    # Registry functions.
    'Get-RegistrySubKeyNames'
    'Get-RegistryValue'
    'Get-RegistryValueNames'
)
