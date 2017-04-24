function Get-VisualStudio_15_0 {
    [CmdletBinding()]
    param()

    try {
        # Query for the latest 15.0.* version.
        #
        # Note, even though VS 15 Update 1 is sometimes referred to as "15.1", the actual installation
        # version number is 15.0.26403.7.
        #
        # Also note, the capability is registered as VisualStudio_15.0, so the following code should
        # query for 15.0.* versions only.
        Write-Host "Getting latest Visual Studio 15 setup instance."
        $output = New-Object System.Text.StringBuilder
        Write-Host "& $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[15.0,15.1)' -latest -format json"
        & $PSScriptRoot\..\..\..\externals\vswhere\vswhere.exe -version '[15.0,15.1)' -latest -format json 2>&1 |
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
            return (ConvertFrom-Json -InputObject $output.ToString()) |
                Select-Object -First 1
        }
    } catch {
        Write-Host ($_ | Out-String)
    }
}
