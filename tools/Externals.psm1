$NODE_URL = "https://nodejs.org/dist"
$NODE12_VERSION = "12.13.1"




function GetExternalTool {
    param (
        [string]
        $DownloadSource,
        [string]
        $Target,
        [switch]
        $FixNestedDir
    )
    Import-Module -Name "$PSScriptRoot/Shared.psm1"
    $targetDir = "$LAYOUT_DIR/externals/$Target"
    $uri = [uri]::new($DownloadSource)
    $relativeUrl = $uri.Host + $uri.AbsolutePath
    
    # Check if the download already exists.
    $downloadTarget = "$DOWNLOAD_DIR/$relativeUrl"
    $downloadBasename = [System.IO.Path]::GetFileName($downloadTarget)
    $downloadDir = [System.IO.Path]::GetDirectoryName($downloadTarget)
    
    if (Test-Path $downloadTarget) {
        Write-Host "Download exists: $downloadBasename"
    }
    else {
        Write-Host "Downloading $DownloadSource to $downloadTarget"
        New-Item $downloadDir -ItemType Directory -Force | Out-Null
        New-Item $targetDir -ItemType Directory | Out-Null
        Invoke-WebRequest -SkipCertificateCheck -OutFile $downloadTarget -Uri $DownloadSource
        $nestedDir
        if ($downloadBasename -like "*.zip") {
            Write-Host "Extracting $downloadBasename to $targetDir"
            
            Expand-Archive $downloadTarget $targetDir
            if ($FixNestedDir) {
                # Capture the nested directory path if the fix_nested_dir flag is set.
                $nestedDir = $downloadBasename -replace ".zip", "" 
            }
        }
        elseif ($downloadBasename -like "*.tar.gz") {
            Write-Host "Extracting $downloadBasename to $targetDir"
            tar xzf "$downloadTarget" -C "$targetDir"
            if ($FixNestedDir) {
                # Capture the nested directory path if the fix_nested_dir flag is set.
                $nestedDir = $downloadBasename -replace ".tar.gz", "" 
            }
        }
        else {
            # Copy the file.
            Copy-Item $downloadTarget "$targetDir/" -Recurse | Out-Null
        }
    }
    if ($null -ne $nestedDir) {
        # Fixup the nested directory.
        if ( Test-Path "$targetDir/$nestedDir" ) {
            Move-Item "$targetDir/$nestedDir/*" "$targetDir/"
            Remove-Item "$targetDir/$nestedDir"
        }
    }
}

function Get-Externals {
    param (
        [string]
        $RID
    )
    # Download the external tools only for Windows.
    if ( $RID -eq "win-x64" ) {
        GetExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-win-x64.zip" node12 -FixNestedDir
    }
    # Download the external tools only for OSX.
    if ( $RID -eq "osx-x64" ) {
        GetExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-darwin-x64.tar.gz" node12 -FixNestedDir
    }

    # Download the external tools for Linux Runtimes.
    if ( $RID -eq "linux-x64" ) {
        GetExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-linux-x64.tar.gz" node12 -FixNestedDir
        GetExternalTool "https://vstsagenttools.blob.core.windows.net/tools/nodejs/${NODE12_VERSION}/alpine/x64/node-${NODE12_VERSION}-alpine-x64.tar.gz" node12_alpine
    }

    if ( $RID -eq "linux-arm64" ) {
        GetExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-linux-arm64.tar.gz" node12 -FixNestedDir
    }

    if ( $RID -eq "linux-arm" ) {
        GetExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-linux-armv7l.tar.gz" node12 -FixNestedDir
    }
}
Export-ModuleMember -Function @("Get-Externals")
