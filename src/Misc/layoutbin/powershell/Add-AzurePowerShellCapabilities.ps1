[CmdletBinding()]
param()

$script:capabilityName = "AzurePS"

function Get-FromModulePath {
    [CmdletBinding()]
    param([switch]$Classic)

    # Determine which module to look for.
    if ($Classic) {
        $name = "Azure"
    } else {
        $name = "AzureRM"
    }

    # Attempt to resolve the module.
    Write-Host "Attempting to find the module '$name' from the module path."
    $module = Get-Module -Name $name -ListAvailable | Select-Object -First 1
    if (!$module) {
        Write-Host "Not found."
        return $false
    }

    if (!$Classic) {
        # For AzureRM, validate the AzureRM.profile module can be found as well.
        $profileName = "AzureRM.profile"
        Write-Host "Attempting to find the module $profileName"
        $profileModule = Get-Module -Name $profileName -ListAvailable | Select-Object -First 1
        if (!$profileModule) {
            Write-Host "Not found."
            return $false
        }
    }

    # Add the capability.
    Write-Capability -Name $script:capabilityName -Value $module.Version
    return $true
}

function Get-FromSdkPath {
    [CmdletBinding()]
    param([switch]$Classic)

    if ($Classic) {
        $partialPath = 'Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Azure.psd1'
    } else {
        $partialPath = 'Microsoft SDKs\Azure\PowerShell\ResourceManager\AzureResourceManager\AzureRM.Profile\AzureRM.Profile.psd1'
    }

    foreach ($programFiles in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
        if (!$programFiles) {
            continue
        }

        $path = [System.IO.Path]::Combine($programFiles, $partialPath)
        Write-Host "Checking if path exists: $path"
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            # Get the module.
            Write-Host "Get-Module -Name $path"
            $module = Get-Module -Name $path

            # Add the capability.
            Write-Capability -Name $script:capabilityName -Value $module.Version
            return $true
        }
    }

    return $false
}

Write-Host "Env:PSModulePath: '$env:PSMODULEPATH'"
$null = (Get-FromModulePath -Classic:$false) -or
    (Get-FromSdkPath -Classic:$false) -or
    (Get-FromModulePath -Classic:$true) -or
    (Get-FromSdkPath -Classic:$true)
