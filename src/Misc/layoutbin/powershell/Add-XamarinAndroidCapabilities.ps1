[CmdletBinding()]
param()

if (!(Add-CapabilityFromRegistry -Name 'Xamarin.Android' -Hive 'LocalMachine' -View 'Registry32' -KeyName 'Software\Novell\Mono for Android' -ValueName 'InstalledVersion')) {
    $vs15 = Get-VisualStudio_15_0
    if ($vs15 -and $vs15.installationPath) {
        # End with "\" for consistency with old ShellFolder values.
        $shellFolder15 = $vs15.installationPath.TrimEnd('\'[0]) + "\"
        $xamarinAndroidDir = ([System.IO.Path]::Combine($shellFolder15, 'MSBuild', 'Xamarin', 'Android')) + '\'
        if ((Test-Container -LiteralPath $xamarinAndroidDir)) {
            $versionFile = ([System.IO.Path]::Combine($xamarinAndroidDir, 'Version'))
            $version = Get-Content -ErrorAction ignore -TotalCount 1 -LiteralPath $versionFile 
            if ($version) {
                Write-Capability -Name 'Xamarin.Android' -Value $version.trim()
            }
        }
    }
}

