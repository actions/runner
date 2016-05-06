[CmdletBinding()]
param()

$windowsSdks = @( )

# Get the Windows SDK version sub-key names.
$windowsSdkKeyName = 'Software\Microsoft\Microsoft SDKs\Windows'
$versionSubKeyNames =
    Get-RegistrySubKeyNames -Hive 'LocalMachine' -View 'Registry32' -KeyName $windowsSdkKeyName |
    Where-Object { $_ -clike 'v*A' }
foreach ($versionSubKeyName in $versionSubKeyNames) {
    # Parse the version.
    $version = $null
    if (!([System.Version]::TryParse($versionSubKeyName.Substring(1, $versionSubKeyName.Length - 2), [ref]$version))) {
        continue
    }

    # Get the installation folder.
    $versionKeyName = "$windowsSdkKeyName\$versionSubKeyName"
    $installationFolder = Get-RegistryValue -Hive 'LocalMachine' -View 'Registry32' -KeyName $versionKeyName -Value 'InstallationFolder'
    if (!$installationFolder) {
        continue
    }

    # Add the Windows SDK capability.
    $installationFolder = $installationFolder.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $windowsSdkCapabilityName = ("WindowsSdk_{0}.{1}" -f $version.Major, $version.Minor)
    Write-Capability -Name $windowsSdkCapabilityName -Value $installationFolder

    # Add the Windows SDK info as an object with properties (for sorting).
    $windowsSdks += New-Object psobject -Property @{
        InstallationFolder = $installationFolder
        Version = $version
    }

    # Get the NetFx sub-key names.
    $netFxSubKeyNames =
        Get-RegistrySubKeyNames -Hive 'LocalMachine' -View 'Registry32' -KeyName $versionKeyName |
        Where-Object { $_ -clike '*NetFx*x86' -or $_ -clike '*NetFx*x64' }
    foreach ($netFxSubKeyName in $netFxSubKeyNames) {
        # Get the installation folder.
        $netFxKeyName = "$versionKeyName\$netFxSubKeyName"
        $installationFolder = Get-RegistryValue -Hive 'LocalMachine' -View 'Registry32' -KeyName $netFxKeyName -Value 'InstallationFolder'
        if (!$installationFolder) {
            continue
        }

        $installationFolder = $installationFolder.TrimEnd([System.IO.Path]::DirectorySeparatorChar)

        # Add the NetFx tool capability.
        $toolName = $netFxSubKeyName.Substring($netFxSubKeyName.IndexOf('NetFx')) # Trim before "NetFx".
        $toolName = $toolName.Substring(0, $toolName.Length - '-x86'.Length) # Trim the trailing "-x86"/"-x64".
        if ($netFxSubKeyName -clike '*x86') {
            $netFxCapabilityName = "$($windowsSdkCapabilityName)_$toolName"
        } else {
            $netFxCapabilityName = "$($windowsSdkCapabilityName)_$($toolName)_x64"
        }

        Write-Capability -Name $netFxCapabilityName -Value $installationFolder
    }
}

# Add a capability for the max.
$maxWindowsSdk =
    $windowsSdks |
    Sort-Object -Property Version -Descending |
    Select-Object -First 1
if ($maxWindowsSdk) {
    Write-Capability -Name 'WindowsSdk' -Value $maxWindowsSdk.InstallationFolder
}
