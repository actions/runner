[CmdletBinding()]
param (
    [string]
    $RID,
    [ValidateSet("Debug", "Release")]
    [string]
    $Config = "Release"
)
$ErrorActionPreference = "Stop"

Import-Module -Name "$PSScriptRoot/Shared.psm1"
if ([string]::IsNullOrEmpty($RID)) {
    $RID = Get-CurrentOSRuntimeIdentifier
}

Write-Heading "Create layout"
Copy-Item -Path "$SRC_DIR/Misc/layoutroot/*" -Destination $LAYOUT_DIR -Recurse -Force
Copy-Item -Path "$SRC_DIR/Misc/layoutbin/*" -Destination "$LAYOUT_DIR/bin" -Recurse -Force
if ($IsWindows) { Remove-Item @("$LAYOUT_DIR/*.sh", "$LAYOUT_DIR/bin/RunnerService.js") }
if ($IsLinux -or $IsMacOS ) {
    Remove-Item "$LAYOUT_DIR/*.cmd"
    chmod +x "$LAYOUT_DIR/bin/Runner.Listener"
    chmod +x "$LAYOUT_DIR/bin/Runner.Worker"
    chmod +x "$LAYOUT_DIR/bin/Runner.PluginHost"
    chmod +x "$LAYOUT_DIR/bin/installdependencies.sh"
}
Write-Heading "Setup externals folder for $RID runner's layout"
Import-Module -Name "$PSScriptRoot/Externals.psm1"
Get-Externals $RID
Remove-Module -Name "Externals"
Remove-Module -Name "Shared"