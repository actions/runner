[CmdletBinding()]
param()

Write-Host "Checking: env:JAVA_HOME"
if (!$env:JAVA_HOME) {
    Write-Host "Value not found or empty."
    return
}

Add-CapabilityFromEnvironment -Name 'maven' -ValueName 'M2_HOME'
