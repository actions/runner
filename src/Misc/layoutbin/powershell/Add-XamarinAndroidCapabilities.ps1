[CmdletBinding()]
param()

if (!(Add-CapabilityFromRegistry -Name 'Xamarin.Android' -Hive 'LocalMachine' -View 'Registry32' -KeyName 'Software\Novell\Mono for Android' -ValueName 'InstalledVersion')) {
    $vs15 = Get-VisualStudio_15_0
    if ($vs15 -and $vs15.installationPath) {
        # End with "\" for consistency with old ShellFolder values.
        $shellFolder15 = $vs15.installationPath.TrimEnd('\'[0]) + "\"
        $xamarinAndroidDir = ([System.IO.Path]::Combine($shellFolder15, 'MSBuild', 'Xamarin', 'Android')) + '\'
        if ((Test-Container -LiteralPath $xamarinAndroidDir)) {
            # Xamarin.Android 7 has a Version file, and this file is renamed to Version.txt in Xamarin.Android 8.x
            foreach ($file in @('Version', 'Version.txt')) {
                $versionFile = ([System.IO.Path]::Combine($xamarinAndroidDir, $file))
                $version = Get-Content -ErrorAction ignore -TotalCount 1 -LiteralPath $versionFile
                if ($version) {
                    Write-Capability -Name 'Xamarin.Android' -Value $version.trim()
                    break
                }
            }
        }
    }
}

