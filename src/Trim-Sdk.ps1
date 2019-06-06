$ErrorActionPreference = "Stop"

# Push-Location -Path .\Sdk
try
{
    # Generate Namespaces.cs
    Write-Host 'Generating Namespaces.cs'
    Remove-Item -Path Sdk\Namespaces.cs -ErrorAction Ignore
    $namespaces = New-Object System.Collections.Generic.HashSet[string]
    $output = findstr /snir /c:"^namespace " Sdk\*.cs
    foreach ($outputLine in ($output.Trim().Replace("`r", "").Split("`n")))
    {
        $namespace = $outputLine.Trim().Split(':')[-1].Split(' ')[-1]
        $namespaces.Add($namespace) | Out-Null
    }

    $namespaces = New-Object System.Collections.Generic.List[string]($namespaces)
    $namespaces.Sort()
    $content = New-Object System.Text.StringBuilder
    foreach ($namespace in $namespaces)
    {
        $content.AppendLine("namespace $namespace") | Out-Null
        $content.AppendLine("{") | Out-Null
        $content.AppendLine("}") | Out-Null
        $content.AppendLine("") | Out-Null
    }
    [System.IO.File]::WriteAllText("$pwd\Sdk\Namespaces.cs", $content.ToString(), (New-Object System.Text.UTF8Encoding($false)))

    # Gather whitelist of files not to delete
    Write-Host 'Gathering whitelist of files not to delete'
    $whitelist = New-Object System.Collections.Generic.HashSet[string]
    $whitelist.Add(((Resolve-Path -Path Sdk\Namespaces.cs).Path)) | Out-Null
    foreach ($file in (Get-ChildItem -Path Sdk\DTExpressions -Recurse -Filter *.cs))
    {
        $whitelist.Add($file.FullName) | Out-Null
    }
    foreach ($file in (Get-ChildItem -Path Sdk\DTLogging -Recurse -Filter *.cs))
    {
        $whitelist.Add($file.FullName) | Out-Null
    }
    foreach ($file in (Get-ChildItem -Path Sdk\DTObjectTemplating -Recurse -Filter *.cs))
    {
        $whitelist.Add($file.FullName) | Out-Null
    }
    foreach ($file in (Get-ChildItem -Path Sdk\DTPipelines\Pipelines\ContextData -Recurse -Filter *.cs))
    {
        $whitelist.Add($file.FullName) | Out-Null
    }
    foreach ($file in (Get-ChildItem -Path Sdk\DTPipelines\Pipelines\ObjectTemplating -Recurse -Filter *.cs))
    {
        $whitelist.Add($file.FullName) | Out-Null
    }

    # Gather candidate files to delete
    Write-Host 'Gathering candidate files to delete'
    $candidatePaths = New-Object System.Collections.Generic.List[string]
    $deletedPaths = New-Object System.Collections.Generic.List[string]
    foreach ($candidateFile in (Get-ChildItem -Path Sdk -Recurse -Filter *.cs))
    {
        if (!$whitelist.Contains($candidateFile.FullName) -and (($candidateFile.FullName.IndexOf('\obj\')) -le 0))
        {
            $candidatePaths.Add($candidateFile.FullName)
        }
    }

    while ($true)
    {
        $found = $false
        for ($i = 0; $i -lt $candidatePaths.Count; )
        {
            $candidatePath = $candidatePaths[$i]
            Write-Host "Checking $candidatePath"
            Remove-Item -Path $candidatePath
            .\dev.cmd build
            if ($LASTEXITCODE -eq 0)
            {
                $deletedPaths.Add($candidatePath)
                $candidatePaths.RemoveAt($i)
                Write-Host "Successfully deleted $candidatePath"
                $found = $true
            }
            else
            {
                Write-Host "Undeleting $candidatePath"
                git checkout -- $candidatePath
                $i++
            }
        }

        if (!$found)
        {
            break;
        }
    }
}
finally
{
    # Pop-Location
}