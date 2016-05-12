[CmdletBinding()]
param()

$rootsKeyName = 'Software\Microsoft\Windows Kits\Installed Roots'
$valueNames = Get-RegistryValueNames -Hive 'LocalMachine' -View 'Registry32' -KeyName $rootsKeyName
$versionInfos = @( )
foreach ($valueName in $valueNames) {
    if (!"$valueName".StartsWith('KitsRoot', 'OrdinalIgnoreCase')) {
        continue
    }

    $installDirectory = Get-RegistryValue -Hive 'LocalMachine' -View 'Registry32' -KeyName $rootsKeyName -ValueName $valueName
    $splitInstallDirectory =
        "$installDirectory".Split(@( ([System.IO.Path]::DirectorySeparatorChar) ) ) |
        ForEach-Object { "$_".Trim() } |
        Where-Object { $_ }
    $splitInstallDirectory = @( $splitInstallDirectory )
    if ($splitInstallDirectory.Length -eq 0) {
        continue
    }

    $version = $null
    if (!([System.Version]::TryParse($splitInstallDirectory[-1], [ref]$version))) {
        continue
    }

    Write-Capability -Name "WindowsKit_$($version.Major).$($version.Minor)" -Value $installDirectory
    $versionInfos += @{
        Version = $version
        InstallDirectory = $installDirectory
    }
}

if ($versionInfos.Length) {
    $maxInfo =
        $versionInfos |
        Sort-Object -Descending -Property Version |
        Select-Object -First 1
    Write-Capability -Name "WindowsKit" -Value $maxInfo.InstallDirectory
}
