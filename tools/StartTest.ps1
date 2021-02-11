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
Write-Heading "Testing"
dotnet test "$SRC_DIR/Test/Test.csproj" -c $CONFIG -r $RID --logger:trx

Remove-Module -Name Shared