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
if (-not (Test-Path "${LAYOUT_DIR}/bin")) {
    Write-Host "You must build first.  Expecting to find ${LAYOUT_DIR}/bin"
}

# TODO: We are cross-compiling arm on x64 so we cant exec Runner.Listener. Remove after building on native arm host
$runnerVersion = {
    $json = Get-Content "$workSpace/version.json" | ConvertFrom-Json
    return $json.version
}.Invoke()
$runnerPackageName = "actions-runner-${RID}-${runnerVersion}"

Write-Heading "Packaging ${runnerPackageName}"

#Remove-Item -Recurse -Force "${LAYOUT_DIR:?}/_diag" -ErrorAction SilentlyContinue
#Remove .pdb files
Get-ChildItem -Path "$LAYOUT_DIR/bin" -Filter "*.pdb" | Remove-Item

New-Item -Path $PACKAGE_DIR -ItemType Directory 
#rm -Rf "${PACKAGE_DIR:?}"/*
if ( $IsLinux -or $IsMacOS ) {
    $tarName="$runnerPackageName.tar.gz"
    Write-Host "Creating $tarName in $PACKAGE_DIR"
    Get-ChildItem $LAYOUT_DIR
    tar -czPf "$tarName" -C "$LAYOUT_DIR" .
    Get-Item "*.tar.gz"|Move-Item -Destination $PACKAGE_DIR
    Get-ChildItem $PACKAGE_DIR
}elseif ( $IsWindows ) {
    $zipName="$runnerPackageName.zip"
    Write-Host "Creating $zip_name in $PACKAGE_DIR"
    Compress-Archive -Path "$LAYOUT_DIR/*" -DestinationPath "$PACKAGE_DIR/$zipName"|Out-Null
}
Remove-Module -Name "Shared"