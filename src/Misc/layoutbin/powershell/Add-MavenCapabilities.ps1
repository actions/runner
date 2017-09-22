[CmdletBinding()]
param()

Write-Host "Checking: env:JAVA_HOME"
if (!$env:JAVA_HOME) {
    Write-Host "Value not found or empty."
    return
}

if($env:M2_HOME) {
    Add-CapabilityFromEnvironment -Name 'maven' -VariableName 'M2_HOME'
} else {
	Write-Host "M2_HOME not set. Checking in PATH"
    Add-CapabilityFromApplication -Name 'maven' -ApplicationName 'mvn'
}