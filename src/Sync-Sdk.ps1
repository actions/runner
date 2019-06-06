$ErrorActionPreference = "Stop"

$runnerRepo = Read-Host -Prompt "actions/runner repository root"
if (!(Test-Path -LiteralPath "$runnerRepo/src")) {
    Write-Error "$runnerRepo should contains a /src folder"
    return 1
}

$gitHubSdkFolder = Join-Path -Path "$runnerRepo/src" -ChildPath "Sdk"

$vsoRepo = $PWD
while ($true) {
    if (Test-Path -LiteralPath "$vsoRepo/init.cmd") {
        break;
    }
    else {
        $vsoRepo = (Get-Item $vsoRepo).Parent.FullName
    }
}

$targetFolders = @(
    "Common"   
    "WebApi"
    "AadAuthentication"
    "DTContracts"
    "DTGenerated"
    "DTLogging"
    "DTExpressions"
    "DTObjectTemplating"
    "DTPipelines"
    "DTWebApi"
    "Resources"
)

$sourceFolders = @{
    "Vssf\Client\Common"                                                           = "Common";
    "Vssf\Client\WebApi"                                                           = "WebApi";
    "DistributedTask\Shared\Common\Contracts"                                      = "DTContracts";
    "DistributedTask\Client\WebApi\Generated"                                      = "DTGenerated";
    "DistributedTask\Client\WebApi\Logging"                                        = "DTLogging";
    "DistributedTask\Client\WebApi\Expressions"                                    = "DTExpressions";
    "DistributedTask\Client\WebApi\ObjectTemplating"                               = "DTObjectTemplating";
    "DistributedTask\Client\WebApi\Pipelines"                                      = "DTPipelines";
    "DistributedTask\Client\WebApi\WebApi"                                         = "DTWebApi";
    "..\obj\Debug.AnyCPU\Vssf.Client\MS.VS.Services.Common\EmbeddedVersionInfo.cs" = "Common\EmbeddedVersionInfo.cs";
    "Vssf\InteractiveClient\Client\Authentication\VssAadToken.cs"                  = "AadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssAadTokenProvider.cs"          = "AadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssAadCredential.cs"             = "AadAuthentication";
    "Vssf\InteractiveClient\Client\VssAadSettings.cs"                              = "AadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssFederatedCredential.cs"       = "AadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssFederatedToken.cs"            = "AadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssFederatedTokenProvider.cs"    = "AadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\Utility\CookieUtility.cs"        = "AadAuthentication";
    "DistributedTask\Client\WebApi\Pipelines\ObjectTemplating\workflow-v1.0.json"  = "DTPipelines";
}

$extraFiles = @(
    "Common\Common\CommandLine\Argument.cs"
    "Common\Common\CommandLine\AttributeBasedOperationModeHandlerFactory.cs"    
    "Common\Common\CommandLine\AttributeBasedOptionParserAdapter.cs"
    "Common\Common\CommandLine\BasicParser.cs"
    "Common\Common\CommandLine\CommandLineLexer.cs"
    "Common\Common\CommandLine\Enumerations.cs"
    "Common\Common\CommandLine\Exceptions.cs"
    "Common\Common\CommandLine\Extensions.cs"
    "Common\Common\CommandLine\IEnumerable.cs"
    "Common\Common\CommandLine\OperationHandler.cs"
    "Common\Common\CommandLine\OperationHandlerFactory.cs"
    "Common\Common\CommandLine\OperationModeAttribute.cs"
    "Common\Common\CommandLine\Option.cs"
    "Common\Common\CommandLine\OptionAttribute.cs"
    "Common\Common\CommandLine\OptionParser.cs"
    "Common\Common\CommandLine\OptionReader.cs"
    "Common\Common\CommandLine\ResponseFileOptionReader.cs"
    "Common\Common\CommandLine\Validation\DefaultValidation.cs"
    "Common\Common\CommandLine\Validation\IOptionValidation.cs"
    "Common\Common\CommandLine\Validation\OptionExistsFilter.cs"
    "Common\Common\CommandLine\Validation\OptionMustExist.cs"
    "Common\Common\CommandLine\Validation\OptionRequiresSpecificValue.cs"
    "Common\Common\CommandLine\Validation\OptionsAreMutuallyExclusive.cs"
    "Common\Common\CommandLine\Validation\OptionsAreMutuallyInclusive.cs"
    "Common\Common\CommandLine\Validation\OptionValidation.cs"
    "Common\Common\CommandLine\Validation\OptionValidationFilter.cs"
    "Common\Common\CommandLine\Validation\OptionValueFilter.cs"
    "Common\Common\CommandLine\ValueConverters\CsvCollectionConverter.cs"
    "Common\Common\CommandLine\ValueConverters\EnumConverter.cs"
    "Common\Common\CommandLine\ValueConverters\IValueConvertible.cs"
    "Common\Common\CommandLine\ValueConverters\UriConverter.cs"
    "Common\Common\CommandLine\ValueConverters\ValueConverter.cs"
    "Common\Common\ExternalProviders\IExternalProviderHttpRequester.cs"
    "Common\Common\Performance\PerformanceNativeMethods.cs"
    "Common\Common\TokenStorage\RegistryToken.cs"
    "Common\Common\TokenStorage\RegistryTokenStorage.cs"
    "Common\Common\TokenStorage\RegistryTokenStorageHelper.cs"
    "Common\Common\TokenStorage\VssTokenStorageFactory.cs"
    "Common\Common\Utility\CredentialsCacheManager.cs"
    "Common\Common\Utility\EncryptionUtility.cs"
    "Common\Common\Utility\EnumerableUtility.cs"
    "Common\Common\Utility\EnvironmentWrapper.cs"
    "Common\Common\Utility\ExceptionExtentions.cs"
    "Common\Common\Utility\NativeMethods.cs"
    "Common\Common\Utility\OSDetails.cs"
    "Common\Common\Utility\DateTimeUtility.cs"
    "Common\Common\Utility\PasswordUtility.cs"
    "Common\Common\Utility\RegistryHelper.cs"
    "Common\Common\Utility\SerializationHelper.cs"
    "Common\Common\Utility\Csv\CsvException.cs"
    "Common\Common\Utility\Csv\CsvConfiguration.cs"
    "Common\Common\Utility\Csv\CsvWriter.cs"
    "Common\Common\VssEnvironment.cs"
    "WebApi\WebApi\AssemblyAttributes.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\ExpiringToken.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitIssueComparer.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\ExternalGitExtensions.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitPullRequestComparer.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitCommitComparer.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\ExternalGitIssueEvent.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitRepoComparer.cs"
    "WebApi\WebApi\Contracts\ExternalEvent\ExternalGitCommitCommentEvent.cs"
    "WebApi\WebApi\Contracts\PermissionLevel\Client\PagedPermissionLevelAssignment.cs"
    "WebApi\WebApi\Contracts\PermissionLevel\Client\PermissionLevelAssignment.cs"
    "WebApi\WebApi\Contracts\PermissionLevel\Enumerations.cs"
    "WebApi\WebApi\Contracts\PermissionLevel\Client\PermissionLevelDefinition.cs"
    "WebApi\WebApi\Contracts\Tokens\PATAddedEvent.cs"
    "WebApi\WebApi\Contracts\Tokens\SshKeyAddedEvent.cs"
    "WebApi\WebApi\Contracts\Tokens\ExpiringTokenEvent.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\PATAddedEvent.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\SshKeyAddedEvent.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\ExpiringTokenEvent.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\DelegatedAuthMigrationStatus.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\DelegatedAuthorizationMigrationBase.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessKeyPublicDataMigration.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessMigration.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationMigration.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessKeyMigration.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationRegistrationMigration.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationRegistrationRedirectLocationMigration.cs"
    "WebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedHostAuthorizationMigration.cs"
    "WebApi\WebApi\Contracts\OAuthWhitelist\OAuthWhitelistEntry.cs"
    "WebApi\WebApi\Contracts\TokenAdmin\PatRevokedEvent.cs"
    "WebApi\WebApi\Contracts\TokenAdmin\TokenAdministrationRevocation.cs"
    "WebApi\WebApi\Contracts\TokenAdmin\TokenAdminPagedSessionTokens.cs"
    "WebApi\WebApi\Contracts\TokenAdmin\TokenAdminRevocation.cs"
    "WebApi\WebApi\Contracts\TokenAdmin\TokenAdminRevocationRule.cs"
    "WebApi\WebApi\Exceptions\AuditLogExceptions.cs"
    "WebApi\WebApi\Exceptions\AadExceptions.cs"    
    "WebApi\WebApi\Exceptions\PermissionLevelExceptions.cs"
    "WebApi\WebApi\HttpClients\CsmResourceProviderHttpClient.cs"    
    "WebApi\WebApi\HttpClients\Generated\CsmResourceProviderHttpClientBase.cs"
    "WebApi\WebApi\HttpClients\Generated\OAuthWhitelistHttpClient.cs"    
    "WebApi\WebApi\HttpClients\Generated\TokenAdminHttpClient.cs"
    "WebApi\WebApi\HttpClients\Generated\TokenAdministrationHttpClient.cs"    
    "WebApi\WebApi\HttpClients\Generated\TokenExpirationHttpClient.cs"
    "WebApi\WebApi\HttpClients\Generated\TokenMigrationHttpClient.cs"    
    "WebApi\WebApi\HttpClients\Generated\PermissionLevelHttpClient.cs"    
    "WebApi\WebApi\HttpClients\CommerceHostHelperHttpClient.cs"    
    "WebApi\WebApi\Utilities\DelegatedAuthComparers.cs"    
    "WebApi\WebApi\Utilities\HttpHeadersExtensions.cs"    
    "WebApi\WebApi\VssClientCertificateManager.cs"    
    "WebApi\WebApi\VssClientEnvironment.cs"    
    "WebApi\WebApi\VssSoapMediaTypeFormatter.cs"    
)

$resourceFiles = @{
    "ExpressionResources"     = "DistributedTask\Client\WebApi\Expressions\ExpressionResources.resx";
    "PipelineStrings"         = "DistributedTask\Client\WebApi\Pipelines\PipelineStrings.resx";
    "CommonResources"         = "Vssf\Client\Common\Resources.resx";
    "IdentityResources"       = "Vssf\Client\WebApi\Resources\IdentityResources.resx";
    "JwtResources"            = "Vssf\Client\WebApi\Resources\JwtResources.resx";
    "WebApiResources"         = "Vssf\Client\WebApi\Resources\WebApiResources.resx";
    "DataImportResources"     = "Vssf\Client\WebApi\Resources\DataImportResources.resx";
    "PatchResources"          = "Vssf\Client\WebApi\Resources\PatchResources.resx";
    "AccountResources"        = "Vssf\Client\WebApi\Resources\AccountResources.resx";
    "TemplateStrings"         = "DistributedTask\Client\WebApi\ObjectTemplating\TemplateStrings.resx";
    "GraphResources"          = "Vssf\Client\WebApi\Resources\GraphResources.resx";
    "FileContainerResources"  = "Vssf\Client\WebApi\Resources\FileContainerResources.resx";
    "LocationResources"       = "Vssf\Client\WebApi\Resources\LocationResources.resx";
    "CommerceResources"       = "Vssf\Client\WebApi\Resources\CommerceResources.resx";
    "SecurityResources"       = "Vssf\Client\WebApi\Resources\SecurityResources.resx";
    "WebPlatformResources"    = "Vssf\Client\WebApi\Resources\WebPlatformResources.resx";
    "ZeusWebApiResources"     = "Vssf\Client\WebApi\Resources\ZeusWebApiResources.resx";
    "NameResolutionResources" = "Vssf\Client\WebApi\Resources\NameResolutionResources.resx";
    "PartitioningResources"   = "Vssf\Client\WebApi\Resources\PartitioningResources.resx";
}

$resourceNamespace = @{
    "ExpressionResources"     = "Microsoft.TeamFoundation.DistributedTask.Expressions";
    "PipelineStrings"         = "Microsoft.TeamFoundation.DistributedTask.Pipelines";
    "CommonResources"         = "Microsoft.VisualStudio.Services.Common.Internal";
    "IdentityResources"       = "Microsoft.VisualStudio.Services.WebApi";
    "JwtResources"            = "Microsoft.VisualStudio.Services.WebApi";
    "WebApiResources"         = "Microsoft.VisualStudio.Services.WebApi";
    "DataImportResources"     = "Microsoft.VisualStudio.Services.WebApi";
    "PatchResources"          = "Microsoft.VisualStudio.Services.WebApi";
    "AccountResources"        = "Microsoft.VisualStudio.Services.WebApi";
    "TemplateStrings"         = "Microsoft.TeamFoundation.DistributedTask.ObjectTemplating";
    "GraphResources"          = "Microsoft.VisualStudio.Services.WebApi";
    "FileContainerResources"  = "Microsoft.VisualStudio.Services.WebApi";
    "LocationResources"       = "Microsoft.VisualStudio.Services.WebApi";
    "CommerceResources"       = "Microsoft.VisualStudio.Services.WebApi";
    "SecurityResources"       = "Microsoft.VisualStudio.Services.WebApi";
    "WebPlatformResources"    = "Microsoft.VisualStudio.Services.WebApi";
    "ZeusWebApiResources"     = "Microsoft.VisualStudio.Services.WebApi";
    "NameResolutionResources" = "Microsoft.VisualStudio.Services.WebApi";
    "PartitioningResources"   = "Microsoft.VisualStudio.Services.WebApi";
}

foreach ($folder in $targetFolders) {
    Write-Host "Recreate $gitHubSdkFolder\$folder"

    Remove-Item -LiteralPath "$gitHubSdkFolder\$folder" -Force -Recurse
    New-Item -Path $gitHubSdkFolder -Name $folder -ItemType "directory" -Force
}

foreach ($sourceFolder in $sourceFolders.Keys) {
    $copySource = Join-Path -Path $vsoRepo -ChildPath $sourceFolder
    $copyDest = Join-Path -Path $gitHubSdkFolder -ChildPath $sourceFolders[$sourceFolder]
    
    Write-Host "Copy $copySource to $copyDest"

    Copy-Item -Path $copySource -Destination $copyDest -Filter "*.cs" -Recurse -Force 
}

Write-Host "Delete extra none NetStandard files"
foreach ($extraFile in $extraFiles) {
    Remove-Item -LiteralPath "$gitHubSdkFolder\$extraFile" -Force
}

Write-Host "Generate C# file for resx files"
foreach ($resourceFile in $resourceFiles.Keys) {
    Write-Host "Generate file for $resourceFile"
    $stringBuilder = New-Object System.Text.StringBuilder
    $file = $resourceFiles[$resourceFile]
    $xml = [xml](Get-Content -LiteralPath "$vsoRepo\$file")
    $null = $stringBuilder.AppendLine('using System.Globalization;')
    $null = $stringBuilder.AppendLine('')
    $namespace = $resourceNamespace[$resourceFile]
    $null = $stringBuilder.AppendLine("namespace $namespace")
    $null = $stringBuilder.AppendLine('{')
    $null = $stringBuilder.AppendLine("    public static class $resourceFile")
    $null = $stringBuilder.AppendLine('    {')
    foreach ($data in $xml.root.data) {
        $i = 0
        $args = ""
        $inputs = ""
        while ($true) {
            if ($data.value.Contains("{$i}") -or $data.value.Contains("{$i" + ":")) {
                if ($i -eq 0) {
                    $args = "object arg$i"
                    $inputs = "arg$i"
                }
                else {
                    $args = $args + ", " + "object arg$i"    
                    $inputs = $inputs + ", " + "arg$i"
                }
                $i++
            }
            else {
                break
            }
        }
        
        $null = $stringBuilder.AppendLine("")
        $null = $stringBuilder.AppendLine("        public static string $($data.name)($($args))")
        $null = $stringBuilder.AppendLine("        {")
        $null = $stringBuilder.AppendLine(@"
            const string Format = @"$($data.value.Replace('"', '""'))";
"@)
        if ($i -eq 0) {
            $null = $stringBuilder.AppendLine("            return Format;")
        }
        else {
            $null = $stringBuilder.AppendLine("            return string.Format(CultureInfo.CurrentCulture, Format, $inputs);")
        }
        $null = $stringBuilder.AppendLine("        }")
    }

    $null = $stringBuilder.AppendLine("    }")
    $null = $stringBuilder.AppendLine("}")

    # Write Resources.g.cs.
    $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "Resources\$resourceFile.g.cs"
    [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))
}

# Print out all namespaces
Write-Host "Rename namespaces:"
$namespaces = New-Object 'System.Collections.Generic.HashSet[string]'
$sourceFiles = Get-ChildItem -LiteralPath $gitHubSdkFolder -Filter "*.cs" -Recurse -Force -File
foreach ($file in $sourceFiles) {
    foreach ($line in Get-Content $file.FullName) {
        if ($line.StartsWith("namespace ")) {
            $namespace = $line.Substring("namespace ".Length)
            if ($namespaces.Add($namespace)) {
                Write-Host $namespace
            }
        }
    }
}

# Rename all namespaces to GitHub
$allSourceFiles = Get-ChildItem -LiteralPath $gitHubSdkFolder -Filter "*.cs" -Recurse -Force -File
foreach ($file in $allSourceFiles) {
    $stringBuilder = New-Object System.Text.StringBuilder
    foreach ($line in Get-Content $file.FullName) {
        if ($line.Contains("Microsoft.VisualStudio")) {
            $line = $line.Replace("Microsoft.VisualStudio", "GitHub");
        }
        elseif ($line.Contains("Microsoft.Azure.DevOps")) {
            $line = $line.Replace("Microsoft.Azure.DevOps", "GitHub");
        }
        elseif ($line.Contains("Microsoft.TeamFoundation")) {
            $line = $line.Replace("Microsoft.TeamFoundation", "GitHub");
        }

        $null = $stringBuilder.AppendLine($line)
    }

    [System.IO.File]::WriteAllText($file.FullName, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))
}

Write-Host "Done"