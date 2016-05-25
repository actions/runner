[CmdletBinding()]
param()

Write-Host "Checking: env:JAVA_HOME"
if (!$env:JAVA_HOME) {
    Write-Host "Value not found or empty."
    return
}

Add-CapabilityFromEnvironment -Name 'maven' -VariableName 'M2_HOME'
