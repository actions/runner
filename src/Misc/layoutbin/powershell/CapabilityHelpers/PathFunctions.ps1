function Test-Container {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$LiteralPath)

    Write-Host "Testing container: '$LiteralPath'"
    if ((Test-Path -LiteralPath $LiteralPath -PathType Container)) {
        Write-Host 'Exists.'
        return $true
    }

    Write-Host 'Does not exist.'
    return $false
}

function Test-Leaf {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$LiteralPath)

    Write-Host "Testing leaf: '$LiteralPath'"
    if ((Test-Path -LiteralPath $LiteralPath -PathType Leaf)) {
        Write-Host 'Exists.'
        return $true
    }

    Write-Host 'Does not exist.'
    return $false
}
