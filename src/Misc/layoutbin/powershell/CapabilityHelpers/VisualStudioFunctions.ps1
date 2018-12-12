function Get-VisualStudio {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet(15, 16)]
        [int]$MajorVersion)

    try {
        # Query for the latest 15.*/16.* version.
        #
        # Note, the capability is registered as VisualStudio_15.0/VisualStudio_16.0, however the actual
        # version may something like 15.2/16.2.
        Write-Host "Getting latest Visual Studio $MajorVersion setup instance."
        $output = New-Object System.Text.StringBuilder
        Write-Host "& $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[$MajorVersion.0,$($MajorVersion+1).0)' -latest -format json"
        & $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version "[$MajorVersion.0,$($MajorVersion+1).0)" -latest -format json 2>&1 |
            ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord]) {
                    Write-Host "STDERR: $($_.Exception.Message)"
                }
                else {
                    Write-Host $_
                    $null = $output.AppendLine($_)
                }
            }
        Write-Host "Exit code: $LASTEXITCODE"
        if ($LASTEXITCODE -eq 0) {
            $instance = (ConvertFrom-Json -InputObject $output.ToString()) |
                Select-Object -First 1
            if (!$instance) {
                Write-Host "Getting latest BuildTools $MajorVersion setup instance."
                $output = New-Object System.Text.StringBuilder
                Write-Host "& $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[$MajorVersion.0,$($MajorVersion+1).0)' -products Microsoft.VisualStudio.Product.BuildTools -latest -format json"
                & $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version "[$MajorVersion.0,$($MajorVersion+1).0)" -products Microsoft.VisualStudio.Product.BuildTools -latest -format json 2>&1 |
                    ForEach-Object {
                        if ($_ -is [System.Management.Automation.ErrorRecord]) {
                            Write-Host "STDERR: $($_.Exception.Message)"
                        }
                        else {
                            Write-Host $_
                            $null = $output.AppendLine($_)
                        }
                    }
                Write-Host "Exit code: $LASTEXITCODE"
                if ($LASTEXITCODE -eq 0) {
                    $instance = (ConvertFrom-Json -InputObject $output.ToString()) |
                        Select-Object -First 1
                }
            }

            return $instance
        }
    } catch {
        Write-Host ($_ | Out-String)
    }
}
