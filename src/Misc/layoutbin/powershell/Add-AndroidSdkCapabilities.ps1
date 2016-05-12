[CmdletBinding()]
param()

function Get-AndroidSdkPath {
    # Get ANDROID_HOME environment variable.
    $machineVars = [System.Environment]::GetEnvironmentvariables([System.EnvironmentVariableTarget]::Machine);
    if ($androidHome = $machineVars['ANDROID_HOME']) {
        Write-Host "Found ANDROID_HOME from machine environment."
        return $androidHome
    }

    $userVars = [System.Environment]::GetEnvironmentvariables([System.EnvironmentVariableTarget]::User);
    if ($androidHome = $userVars['ANDROID_HOME']) {
        Write-Host "Found ANDROID_HOME from user environment."
        return $androidHome
    }

    # Get the SDK path from the Android SDK registry info.
    # The Android SDK is always x86. If running as a 32-bit process, checking
    # the SOFTWARE node will suffice. Otherwise, if running as a 64-bit process,
    # we need to check under Wow6432Node.
    $hiveViewPairs = @(
        @{ Hive = 'CurrentUser' ; View = 'Default' }
        @{ Hive = 'LocalMachine' ; View = 'Registry64' }
        @{ Hive = 'LocalMachine' ; View = 'Registry32' }
    )
    foreach ($pair in $hiveViewPairs) {
        $sdkPath = Get-RegistryValue -Hive $pair.Hive -View $pair.View -KeyName "SOFTWARE\Android SDK Tools" -ValueName 'Path'
        $sdkPath = "$sdkPath".Trim()
        if ($sdkPath) {
            return $sdkPath
        }
    }
}

# Check for the SDK path.
$sdkPath = Get-AndroidSdkPath
if (!$sdkPath) {
    return
}

# Output the capability.
Write-Capability -Name 'AndroidSDK' -Value $sdkPath

# Check if the platforms directory exists.
$platformsDirectory = [System.IO.Path]::Combine($sdkPath, "platforms")
if (!(Test-Container -LiteralPath $platformsDirectory)) {
    return
}

# Go through installed platforms and add each to capabilities.
foreach ($item in (Get-ChildItem -LiteralPath $platformsDirectory)) {
    if ($item -is [System.IO.DirectoryInfo]) {
        $sdkName = $item.Name.Replace("android-", "AndroidSDK_")
        Write-Capability -Name $sdkName -Value $item.FullName
    }
}
