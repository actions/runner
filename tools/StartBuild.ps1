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


# # function Package () {
# #     if [ ! -d "${LAYOUT_DIR}/bin" ]; then
# #     Write-Host "You must build first.  Expecting to find ${LAYOUT_DIR}/bin"
# #     fi

# #     # TODO: We are cross-compiling arm on x64 so we cant exec Runner.Listener. Remove after building on native arm host
# #     runner_ver=$("${LAYOUT_DIR}/bin/Runner.Listener" --version) || runner_ver=$(cat runnerversion) || failed "version"
# #     runner_pkg_name="actions-runner-${RUNTIME_ID}-${runner_ver}"

# #     Heading "Packaging ${runner_pkg_name}"

# #     Remove-Item -Rf "${LAYOUT_DIR:?}/_diag"
# #     find "${LAYOUT_DIR}/bin" -type f -name '*.pdb' -delete

# #     mkdir -p "$PACKAGE_DIR"
# #     Remove-Item -Rf "${PACKAGE_DIR:?}"/*

# #     Push-Location "$PACKAGE_DIR" > /dev/null

# #     if [[ ("$CURRENT_PLATFORM" = = "linux") || ("$CURRENT_PLATFORM" = = "darwin") ]]; then
# #     tar_name="${runner_pkg_name}.tar.gz"
# #     Write-Host "Creating $tar_name in ${LAYOUT_DIR}"
# #     tar -czf "${tar_name}" -C "${LAYOUT_DIR}" .
# #     elif [[ ("$CURRENT_PLATFORM" = = "windows") ]]; then
# #     zip_name="${runner_pkg_name}.zip"
# #     Write-Host "Convert ${LAYOUT_DIR} to Windows style path"
# #     window_path=${LAYOUT_DIR:1}
# #     window_path=${window_path:0:1}:${window_path:1}
# #     Write-Host "Creating $zip_name in ${window_path}"
# #     powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "Add-Type -Assembly \"System.IO.Compression.FileSystem\"; [System.IO.Compression.ZipFile]::CreateFromDirectory(\"${window_path}\", \"${zip_name}\")"
# #     fi

# #     Pop-Location > /dev/null
# # }

# switch ($Command) {
#     { $_ -eq "build" || $_ -eq "b" } { Build }
#     { $_ -eq "test" || $_ -eq "b" } { Runtest }
#     { $_ -eq "layout" || $_ -eq "b" } { Layout }
#     # { $_ -eq "package" || $_ -eq "b" } { Package }
#     Default {
#         Write-Host "Invalid cmd.  Use build(b), test(t), layout(l) or package(p)"; ;
#     }
# }

Write-Host
Write-Host "Done."
Write-Host
Remove-Module -Name "Shared"