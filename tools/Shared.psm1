$workSpace = {
    $dir = $PSScriptRoot
    while (-not (($dir | Get-ChildItem -Name) -contains "version.json") ) {
        $dir = [System.IO.Path]::GetDirectoryName($dir)
    }
    $dir
}.Invoke()
$LAYOUT_DIR = [System.IO.Path]::Combine($workSpace, "_layout")
$PACKAGE_DIR=[System.IO.Path]::Combine($workSpace, "_package")
$SRC_DIR = [System.IO.Path]::Combine($workSpace, "src")
$DOWNLOAD_DIR = [System.IO.Path]::Combine($workSpace, "_download")



function Get-CurrentOSRuntimeIdentifier {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
    if ($IsWindows) { return "win-$architecture" }
    if ($IsLinux) { return "linux-$architecture" }
    if ($IsMacOS) { return "osx-$architecture" }
    return "unkonwn"
}    

function Write-Heading {
    param (
        [string]
        $Text
    )
    Write-Host
        Write-Host
        Write-Host "-----------------------------------------"
        Write-Host "$Text..."
        Write-Host "-----------------------------------------"
}
Export-ModuleMember -Function @("Get-CurrentOSRuntimeIdentifier","Write-Heading") -Variable @("workSpace", "LAYOUT_DIR", "SRC_DIR","PACKAGE_DIR","DOWNLOAD_DIR")