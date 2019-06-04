$ErrorActionPreference = "Stop"

$vsoRepo = Read-Host -Prompt "VSO repository root"
if (!(Test-Path -LiteralPath "$vsoRepo/init.cmd")) {
    Write-Error "$vsoRepo should contains the Skyrise V2 init.cmd"
    return 1
}

$targetFolders = @(
    "VssfCommon"   
    "VssfWebApi"
    "VssfAadAuthentication"
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
    "Vssf\Client\Common"                                                           = "VssfCommon";
    "Vssf\Client\WebApi"                                                           = "VssfWebApi";
    "DistributedTask\Shared\Common\Contracts"                                      = "DTContracts";
    "DistributedTask\Client\WebApi\Generated"                                      = "DTGenerated";
    "DistributedTask\Client\WebApi\Logging"                                        = "DTLogging";
    "DistributedTask\Client\WebApi\Expressions"                                    = "DTExpressions";
    "DistributedTask\Client\WebApi\ObjectTemplating"                               = "DTObjectTemplating";
    "DistributedTask\Client\WebApi\Pipelines"                                      = "DTPipelines";
    "DistributedTask\Client\WebApi\WebApi"                                         = "DTWebApi";
    "..\obj\Debug.AnyCPU\Vssf.Client\MS.VS.Services.Common\EmbeddedVersionInfo.cs" = "VssfCommon\EmbeddedVersionInfo.cs";
    "Vssf\InteractiveClient\Client\Authentication\VssAadToken.cs"                  = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssAadTokenProvider.cs"          = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssAadCredential.cs"             = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\VssAadSettings.cs"                              = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssFederatedCredential.cs"       = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssFederatedToken.cs"            = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\VssFederatedTokenProvider.cs"    = "VssfAadAuthentication";
    "Vssf\InteractiveClient\Client\Authentication\Utility\CookieUtility.cs"        = "VssfAadAuthentication";
    "DistributedTask\Client\WebApi\Pipelines\ObjectTemplating\workflow-v1.0.json"  = "DTPipelines";
}

$extraFiles = @(
    "VssfCommon\Common\CommandLine\Argument.cs"
    "VssfCommon\Common\CommandLine\AttributeBasedOperationModeHandlerFactory.cs"    
    "VssfCommon\Common\CommandLine\AttributeBasedOptionParserAdapter.cs"
    "VssfCommon\Common\CommandLine\BasicParser.cs"
    "VssfCommon\Common\CommandLine\CommandLineLexer.cs"
    "VssfCommon\Common\CommandLine\Enumerations.cs"
    "VssfCommon\Common\CommandLine\Exceptions.cs"
    "VssfCommon\Common\CommandLine\Extensions.cs"
    "VssfCommon\Common\CommandLine\IEnumerable.cs"
    "VssfCommon\Common\CommandLine\OperationHandler.cs"
    "VssfCommon\Common\CommandLine\OperationHandlerFactory.cs"
    "VssfCommon\Common\CommandLine\OperationModeAttribute.cs"
    "VssfCommon\Common\CommandLine\Option.cs"
    "VssfCommon\Common\CommandLine\OptionAttribute.cs"
    "VssfCommon\Common\CommandLine\OptionParser.cs"
    "VssfCommon\Common\CommandLine\OptionReader.cs"
    "VssfCommon\Common\CommandLine\ResponseFileOptionReader.cs"
    "VssfCommon\Common\CommandLine\Validation\DefaultValidation.cs"
    "VssfCommon\Common\CommandLine\Validation\IOptionValidation.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionExistsFilter.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionMustExist.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionRequiresSpecificValue.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionsAreMutuallyExclusive.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionsAreMutuallyInclusive.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionValidation.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionValidationFilter.cs"
    "VssfCommon\Common\CommandLine\Validation\OptionValueFilter.cs"
    "VssfCommon\Common\CommandLine\ValueConverters\CsvCollectionConverter.cs"
    "VssfCommon\Common\CommandLine\ValueConverters\EnumConverter.cs"
    "VssfCommon\Common\CommandLine\ValueConverters\IValueConvertible.cs"
    "VssfCommon\Common\CommandLine\ValueConverters\UriConverter.cs"
    "VssfCommon\Common\CommandLine\ValueConverters\ValueConverter.cs"
    "VssfCommon\Common\ExternalProviders\IExternalProviderHttpRequester.cs"
    "VssfCommon\Common\Performance\PerformanceNativeMethods.cs"
    "VssfCommon\Common\TokenStorage\RegistryToken.cs"
    "VssfCommon\Common\TokenStorage\RegistryTokenStorage.cs"
    "VssfCommon\Common\TokenStorage\RegistryTokenStorageHelper.cs"
    "VssfCommon\Common\TokenStorage\VssTokenStorageFactory.cs"
    "VssfCommon\Common\Utility\CredentialsCacheManager.cs"
    "VssfCommon\Common\Utility\EncryptionUtility.cs"
    "VssfCommon\Common\Utility\EnumerableUtility.cs"
    "VssfCommon\Common\Utility\EnvironmentWrapper.cs"
    "VssfCommon\Common\Utility\ExceptionExtentions.cs"
    "VssfCommon\Common\Utility\NativeMethods.cs"
    "VssfCommon\Common\Utility\OSDetails.cs"
    "VssfCommon\Common\Utility\DateTimeUtility.cs"
    "VssfCommon\Common\Utility\PasswordUtility.cs"
    "VssfCommon\Common\Utility\RegistryHelper.cs"
    "VssfCommon\Common\Utility\SerializationHelper.cs"
    "VssfCommon\Common\Utility\Csv\CsvException.cs"
    "VssfCommon\Common\Utility\Csv\CsvConfiguration.cs"
    "VssfCommon\Common\Utility\Csv\CsvWriter.cs"
    "VssfCommon\Common\VssEnvironment.cs"
    "VssfWebApi\WebApi\AssemblyAttributes.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\ExpiringToken.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitIssueComparer.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\ExternalGitExtensions.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitPullRequestComparer.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitCommitComparer.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\ExternalGitIssueEvent.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\Comparers\ExternalGitRepoComparer.cs"
    "VssfWebApi\WebApi\Contracts\ExternalEvent\ExternalGitCommitCommentEvent.cs"
    "VssfWebApi\WebApi\Contracts\PermissionLevel\Client\PagedPermissionLevelAssignment.cs"
    "VssfWebApi\WebApi\Contracts\PermissionLevel\Client\PermissionLevelAssignment.cs"
    "VssfWebApi\WebApi\Contracts\PermissionLevel\Enumerations.cs"
    "VssfWebApi\WebApi\Contracts\PermissionLevel\Client\PermissionLevelDefinition.cs"
    "VssfWebApi\WebApi\Contracts\Tokens\PATAddedEvent.cs"
    "VssfWebApi\WebApi\Contracts\Tokens\SshKeyAddedEvent.cs"
    "VssfWebApi\WebApi\Contracts\Tokens\ExpiringTokenEvent.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\PATAddedEvent.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\SshKeyAddedEvent.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\ExpiringTokenEvent.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\DelegatedAuthMigrationStatus.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\DelegatedAuthorizationMigrationBase.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessKeyPublicDataMigration.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessMigration.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationMigration.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessKeyMigration.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationRegistrationMigration.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationRegistrationRedirectLocationMigration.cs"
    "VssfWebApi\WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedHostAuthorizationMigration.cs"
    "VssfWebApi\WebApi\Contracts\OAuthWhitelist\OAuthWhitelistEntry.cs"
    "VssfWebApi\WebApi\Contracts\TokenAdmin\PatRevokedEvent.cs"
    "VssfWebApi\WebApi\Contracts\TokenAdmin\TokenAdministrationRevocation.cs"
    "VssfWebApi\WebApi\Contracts\TokenAdmin\TokenAdminPagedSessionTokens.cs"
    "VssfWebApi\WebApi\Contracts\TokenAdmin\TokenAdminRevocation.cs"
    "VssfWebApi\WebApi\Contracts\TokenAdmin\TokenAdminRevocationRule.cs"
    "VssfWebApi\WebApi\Exceptions\AuditLogExceptions.cs"
    "VssfWebApi\WebApi\Exceptions\AadExceptions.cs"    
    "VssfWebApi\WebApi\Exceptions\PermissionLevelExceptions.cs"
    "VssfWebApi\WebApi\HttpClients\CsmResourceProviderHttpClient.cs"    
    "VssfWebApi\WebApi\HttpClients\Generated\CsmResourceProviderHttpClientBase.cs"
    "VssfWebApi\WebApi\HttpClients\Generated\OAuthWhitelistHttpClient.cs"    
    "VssfWebApi\WebApi\HttpClients\Generated\TokenAdminHttpClient.cs"
    "VssfWebApi\WebApi\HttpClients\Generated\TokenAdministrationHttpClient.cs"    
    "VssfWebApi\WebApi\HttpClients\Generated\TokenExpirationHttpClient.cs"
    "VssfWebApi\WebApi\HttpClients\Generated\TokenMigrationHttpClient.cs"    
    "VssfWebApi\WebApi\HttpClients\Generated\PermissionLevelHttpClient.cs"    
    "VssfWebApi\WebApi\HttpClients\CommerceHostHelperHttpClient.cs"    
    "VssfWebApi\WebApi\Utilities\DelegatedAuthComparers.cs"    
    "VssfWebApi\WebApi\Utilities\HttpHeadersExtensions.cs"    
    "VssfWebApi\WebApi\VssClientCertificateManager.cs"    
    "VssfWebApi\WebApi\VssClientEnvironment.cs"    
    "VssfWebApi\WebApi\VssSoapMediaTypeFormatter.cs"    
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

$gitHubSdkFolder = Join-Path -Path $PWD -ChildPath "GitHub.Pipelines.Sdk"

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
$allSourceFiles = Get-ChildItem -LiteralPath $PWD -Filter "*.cs" -Recurse -Force -File
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