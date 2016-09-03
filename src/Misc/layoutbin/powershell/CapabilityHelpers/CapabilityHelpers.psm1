[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Import the helper functions.
. $PSScriptRoot\CapabilityFunctions
. $PSScriptRoot\PathFunctions
. $PSScriptRoot\RegistryFunctions
. $PSScriptRoot\VisualStudioFunctions

# Export the public functions.
Export-ModuleMember -Function @(
    # Capability functions.
    'Add-CapabilityFromApplication'
    'Add-CapabilityFromEnvironment'
    'Add-CapabilityFromRegistry'
    'Write-Capability'
    # File system functions with tracing built-in.
    'Test-Container'
    'Test-Leaf'
    # Registry functions.
    'Get-RegistrySubKeyNames'
    'Get-RegistryValue'
    'Get-RegistryValueNames'
    # Visual Studio functions.
    'Get-VisualStudio_15_0'
)
