$vsoRepo = "C:\VSO\src"
$sourceFolders = @(
    "Vssf\Client\Common",
    "Vssf\Client\WebApi",
    "DistributedTask\Shared\Common\Contracts",
    "DistributedTask\Client\WebApi\Generated",
    "DistributedTask\Client\WebApi\Logging",
    "DistributedTask\Client\WebApi\Expressions",
    "DistributedTask\Client\WebApi\ObjectTemplating"
    "DistributedTask\Client\WebApi\Pipelines",
    "DistributedTask\Client\WebApi\WebApi",
    "..\obj\Debug.AnyCPU\Vssf.Client\MS.VS.Services.Common\EmbeddedVersionInfo.cs"
)

$gitHubSdkFolder = Join-Path -Path $PWD -ChildPath ".\GitHub.Pipelines.Sdk"

# foreach ($sourceFolder in $sourceFolders) {
#     $sourceFolder = Join-Path -Path $vsoRepo -ChildPath $sourceFolder

#     $sourceFolder

#     # Get-ChildItem -Path $sourceFolder -Recurse 
    

#     Copy-Item -Path $sourceFolder -Destination $gitHubSdkFolder -Filter "*.cs" -Recurse -Force
# }

Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/VssAadToken.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/VssAadTokenProvider.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/VssAadCredential.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/VssAadSettings.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/VssFederatedCredential.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/VssFederatedToken.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/VssFederatedTokenProvider.cs" -Destination $gitHubSdkFolder -Force
Copy-Item -Path "$vsoRepo/Vssf/InteractiveClient/Client/Authentication/Utility/CookieUtility.cs" -Destination $gitHubSdkFolder -Force

Copy-Item -Path "$vsoRepo/DistributedTask/Client/WebApi/Pipelines/ObjectTemplating/workflow-v1.0.json" -Destination $gitHubSdkFolder -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Argument.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\AttributeBasedOperationModeHandlerFactory.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\AttributeBasedOptionParserAdapter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\BasicParser.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\CommandLineLexer.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Enumerations.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Exceptions.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Extensions.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\IEnumerable.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\OperationHandler.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\OperationHandlerFactory.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\OperationModeAttribute.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Option.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\OptionAttribute.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\OptionParser.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\OptionReader.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\ResponseFileOptionReader.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\DefaultValidation.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\IOptionValidation.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionExistsFilter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionMustExist.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionRequiresSpecificValue.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionsAreMutuallyExclusive.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionsAreMutuallyInclusive.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionValidation.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionValidationFilter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\Validation\OptionValueFilter.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\ValueConverters\CsvCollectionConverter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\ValueConverters\EnumConverter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\ValueConverters\IValueConvertible.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\ValueConverters\UriConverter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\CommandLine\ValueConverters\ValueConverter.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\ExternalProviders\IExternalProviderHttpRequester.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Performance\PerformanceNativeMethods.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\TokenStorage\RegistryToken.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\TokenStorage\RegistryTokenStorage.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\TokenStorage\RegistryTokenStorageHelper.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\TokenStorage\VssTokenStorageFactory.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\CredentialsCacheManager.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\EncryptionUtility.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\EnumerableUtility.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\EnvironmentWrapper.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\ExceptionExtentions.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\NativeMethods.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\OSDetails.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\DateTimeUtility.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\PasswordUtility.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\RegistryHelper.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\SerializationHelper.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\Csv\CsvException.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\Csv\CsvConfiguration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\Utility\Csv\CsvWriter.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "Common\VssEnvironment.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\AssemblyAttributes.cs") -Force

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\ExpiringToken.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\Comparers\ExternalGitIssueComparer.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\ExternalGitExtensions.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\Comparers\ExternalGitPullRequestComparer.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\Comparers\ExternalGitCommitComparer.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\ExternalGitIssueEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\Comparers\ExternalGitRepoComparer.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\ExternalEvent\ExternalGitCommitCommentEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\PermissionLevel\Client\PagedPermissionLevelAssignment.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\PermissionLevel\Client\PermissionLevelAssignment.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\PermissionLevel\Enumerations.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\PermissionLevel\Client\PermissionLevelDefinition.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\Tokens\PATAddedEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\Tokens\SshKeyAddedEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\Tokens\ExpiringTokenEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\PATAddedEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\SshKeyAddedEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\ExpiringTokenEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\DelegatedAuthMigrationStatus.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\DelegatedAuthorizationMigrationBase.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessKeyPublicDataMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationAccessKeyMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationRegistrationMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedAuthorizationRegistrationRedirectLocationMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\DelegatedAuthorization\Migration\TokenDelegatedHostAuthorizationMigration.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\OAuthWhitelist\OAuthWhitelistEntry.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\TokenAdmin\PatRevokedEvent.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\TokenAdmin\TokenAdministrationRevocation.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\TokenAdmin\TokenAdminPagedSessionTokens.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\TokenAdmin\TokenAdminRevocation.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Contracts\TokenAdmin\TokenAdminRevocationRule.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Exceptions\AuditLogExceptions.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Exceptions\AadExceptions.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Exceptions\PermissionLevelExceptions.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\CsmResourceProviderHttpClient.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\CsmResourceProviderHttpClientBase.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\OAuthWhitelistHttpClient.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\TokenAdminHttpClient.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\TokenAdministrationHttpClient.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\TokenExpirationHttpClient.cs") -Force
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\TokenMigrationHttpClient.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\Generated\PermissionLevelHttpClient.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\HttpClients\CommerceHostHelperHttpClient.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Utilities\DelegatedAuthComparers.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\Utilities\HttpHeadersExtensions.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\VssClientCertificateManager.cs") -Force    

# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\VssClientEnvironment.cs") -Force    
# Remove-Item -Path (Join-Path -Path $gitHubSdkFolder -ChildPath "WebApi\VssSoapMediaTypeFormatter.cs") -Force    



# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\DistributedTask\Client\WebApi\Expressions\ExpressionResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.TeamFoundation.DistributedTask.Expressions')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class ExpressionResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "ExpressionResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))

# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\DistributedTask\Client\WebApi\Pipelines\PipelineStrings.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.TeamFoundation.DistributedTask.Pipelines')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class PipelineStrings')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "PipelineStrings.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))

# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\Common\Resources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.Common.Internal')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class CommonResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "CommonResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\IdentityResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class IdentityResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "IdentityResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\JwtResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class JwtResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "JwtResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))



# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\WebApiResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class WebApiResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "WebApiResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))



# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\DataImportResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class DataImportResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "DataImportResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\PatchResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class PatchResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "PatchResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))



# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\AccountResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class AccountResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "AccountResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))



# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\DistributedTask\Client\WebApi\ObjectTemplating\TemplateStrings.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class TemplateStrings')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "TemplateStrings.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))




# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\GraphResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class GraphResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "GraphResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\FileContainerResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class FileContainerResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "FileContainerResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\LocationResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class LocationResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "LocationResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\CommerceResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class CommerceResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "CommerceResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\SecurityResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class SecurityResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "SecurityResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\WebPlatformResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class WebPlatformResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "WebPlatformResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))

# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\ZeusWebApiResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class ZeusWebApiResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "ZeusWebApiResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))



# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\NameResolutionResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class NameResolutionResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "NameResolutionResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))


# ###########################################################

# $stringBuilder = New-Object System.Text.StringBuilder
# $xml = [xml](Get-Content -LiteralPath "$vsoRepo\Vssf\Client\WebApi\Resources\PartitioningResources.resx")
# $null = $stringBuilder.AppendLine('using System.Globalization;')
# $null = $stringBuilder.AppendLine('')
# $null = $stringBuilder.AppendLine('namespace Microsoft.VisualStudio.Services.WebApi')
# $null = $stringBuilder.AppendLine('{')
# $null = $stringBuilder.AppendLine('    public static class PartitioningResources')
# $null = $stringBuilder.AppendLine('    {')
# foreach ($data in $xml.root.data) {
#     $null = $stringBuilder.AppendLine(@"
#         public static string $($data.name)(params object[] args)
#         {
#             const string Format = @"$($data.value.Replace('"', '""'))";
#             if (args == null || args.Length == 0)
#             {
#                 return Format;
#             }
#             return string.Format(CultureInfo.CurrentCulture, Format, args);
#         }
# "@)
# }

# $null = $stringBuilder.AppendLine('    }')
# $null = $stringBuilder.AppendLine('}')

# # Write Resources.g.cs.
# $genResourceFile = Join-Path -Path $gitHubSdkFolder -ChildPath "PartitioningResources.g.cs"
# [System.IO.File]::WriteAllText($genResourceFile, ($stringBuilder.ToString()), ([System.Text.Encoding]::UTF8))

