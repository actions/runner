[CmdletBinding()]
param (
    [string]
    $RID,
    [ValidateSet("Debug", "Release")]
    [string]
    $Config = "Release"
)

&"$PSScriptRoot/tools/StartBuild.ps1" $RID $Config
&"$PSScriptRoot/tools/StartLayout.ps1" $RID $Config
