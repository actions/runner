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
Write-Heading "Build"
dotnet clean "$SRC_DIR/ActionsRunner.sln"
dotnet restore "$SRC_DIR/ActionsRunner.sln"
dotnet publish -r $RID -c $Config --output "$LAYOUT_DIR/bin" "$SRC_DIR/ActionsRunner.sln"
if ($IsWindows ) {
    msbuild -t:restore -p:RuntimeIdentifier=win -p:Configuration=$Config "$SRC_DIR/Runner.Service/Windows/RunnerService.csproj" 
    msbuild -t:build -p:RuntimeIdentifier=win -p:Configuration=$Config "$SRC_DIR/Runner.Service/Windows/RunnerService.csproj" 
}

Write-Host
Write-Host "Done."
Write-Host
Remove-Module -Name "Shared"