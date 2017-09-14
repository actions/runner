[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$VsoSrcPath,
    
    [switch]$SkipCopy)

$ErrorActionPreference = 'Stop'

# Build the TaskResources.g.cs content.
$stringBuilder = New-Object System.Text.StringBuilder
$xml = [xml](Get-Content -LiteralPath "$VsoSrcPath\DistributedTask\Sdk\Server\TaskResources.resx")
$null = $stringBuilder.AppendLine('using System.Globalization;')
$null = $stringBuilder.AppendLine('')
$null = $stringBuilder.AppendLine('namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines')
$null = $stringBuilder.AppendLine('{')
$null = $stringBuilder.AppendLine('    internal static class TaskResources')
$null = $stringBuilder.AppendLine('    {')
foreach ($data in $xml.root.data) {
    $null = $stringBuilder.AppendLine(@"
            internal static string $($data.name)(params object[] args)
            {
                const string Format = @"$($data.value.Replace('"', '""'))";
                if (args == null || args.Length == 0)
                {
                    return Format;
                }

                return string.Format(CultureInfo.CurrentCulture, Format, args);
            }
"@)
}

$null = $stringBuilder.AppendLine('    }')
$null = $stringBuilder.AppendLine('}')

# Copy over the .cs files
if (!$SkipCopy) {
    mkdir $PSScriptRoot\Yaml -ErrorAction Ignore
    robocopy $VsoSrcPath\DistributedTask\Sdk\Server\Pipelines $PSScriptRoot\Yaml *.cs /mir
}

# Write TaskResources.cs.
[System.IO.File]::WriteAllText("$PSScriptRoot\TaskResources.g.cs", ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))