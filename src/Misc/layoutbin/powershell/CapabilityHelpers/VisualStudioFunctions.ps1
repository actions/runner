function Get-VisualStudio_15_0 {
    [CmdletBinding()]
    param()

    try {
        # Query for the latest 15.* version.
        #
        # Note, the capability is registered as VisualStudio_15.0, however the actual
        # version may something like 15.2.
        Write-Host "Getting latest Visual Studio 15 setup instance."
        $output = New-Object System.Text.StringBuilder
        Write-Host "& $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[15.0,16.0)' -latest -format json"
        & $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[15.0,16.0)' -latest -format json 2>&1 |
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
                Write-Host "Getting latest BuildTools 15 setup instance."
                $output = New-Object System.Text.StringBuilder
                Write-Host "& $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[15.0,16.0)' -products Microsoft.VisualStudio.Product.BuildTools -latest -format json"
                & $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[15.0,16.0)' -products Microsoft.VisualStudio.Product.BuildTools -latest -format json 2>&1 |
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
