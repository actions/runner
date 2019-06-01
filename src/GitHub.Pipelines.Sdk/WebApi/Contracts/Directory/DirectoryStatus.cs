namespace Microsoft.VisualStudio.Services.Directories
{
    public static class DirectoryStatus
    {
        /// <summary>
        /// Returned when we failed to read AAD objects
        /// because we couldn't communicate with AAD or because AAD returned an invalid response,
        /// i.e. when AadService.Get(Users/Groups) fails.
        /// </summary>
        public const string AadReadFailed = "AadReadFailed";

        /// <summary>
        /// Returned when the type of entity returned from AAD does not match a known type such as AadUser or AadGroup.
        /// </summary>
        public const string AadReadTypeUnrecognized = "AadReadTypeUnrecognized";

        /// <summary>
        /// Returned when we received a valid response from one backing directory but were unable to convert it to the format required by the target directory.
        /// For example, occurs when we cannot convert an AAD user to the corresponding VSD identity.
        /// </summary>
        public const string ConvertFailed = "ConvertFailed";

        /// <summary>
        /// Returned when we cannot assign the requested local ID because it would cause a conflict with the ID of a preexisting identity.
        /// </summary>
        public const string IdConflict = "IdConflict";

        /// <summary>
        /// Returned when we cannot create an identity ID translation
        /// because there is a conflict with a preexisting identity in the same account scope.
        /// </summary>
        public const string IdTranslationConflict = "IdTranslationConflict";

        /// <summary>
        /// Returned when we failed to create an identity ID translation due to an unexpected error.
        /// </summary>
        public const string IdTranslationFailed = "IdTranslationFailed";

        /// <summary>
        /// Returned when we failed to update storage key translation due to an unexpected error.
        /// </summary>
        public const string UpdateStorageKeysFailed = "UpdateStorageKeysFailed";

        /// <summary>
        /// Returned when we failed to set master id due to an unexpected error.
        /// </summary>
        public const string SetMasterIdFailed = "SetMasterIdFailed";

        /// <summary>
        /// Returned when the number of identities that we expected to assigned storage key is not the same as the number that were actually assigned.
        /// </summary>
        public const string StorageKeyAssignmentResultCountMismatch = "StorageKeyAssignmentResultCountMismatch";

        /// <summary>
        /// Returned when an identity read returned null after we attempted to assign storage key,
        /// which indicates that the storage key assignment was not done successfully.
        /// </summary>
        public const string StorageKeyAssignmentResultNull = "StorageKeyAssignmentResultNull";

        /// <summary>
        /// Returned when we attempted to assign storage key but the resulting local ID did not match the expected value.
        /// Indicates an internal assign storage key failure.
        /// </summary>
        public const string StorageKeyAssignmentLocalIdMismatch = "StorageKeyAssignmentLocalIdMismatch";

        /// <summary>
        /// Returned when we attempted to assign storage key but the resulting master ID did not match the expected value.
        /// Indicates an internal assign storage key failure.
        /// </summary>
        public const string StorageKeyAssignmentMasterIdMismatch = "StorageKeyAssignmentMasterIdMismatch";

        /// <summary>
        /// Returned when we failed to get or create storage key translation due to an unexpected error.
        /// </summary>
        public const string GetOrCreateIdForCuidFailed = "GetOrCreateIdForCuidFailed";

        /// <summary>
        /// Returned when we attempted to create an identity ID translation but the resulting local ID did not match the expected value.
        /// Indicates an internal ID translation failure.
        /// </summary>
        public const string IdTranslationLocalIdMismatch = "IdTranslationLocalIdMismatch";

        /// <summary>
        /// Returned when we attempted to create an identity ID translation but the resulting master ID did not match the expected value.
        /// Indicates an internal ID translation failure.
        /// </summary>
        public const string IdTranslationMasterIdMismatch = "IdTranslationMasterIdMismatch";

        /// <summary>
        /// Returned when we attempted to create an identity ID translation for an identity for which ID translation is not allowed.
        /// </summary>
        public const string IdTranslationNotAllowed = "IdTranslationNotAllowed";

        /// <summary>
        /// Returned when the number of identities that we expected to translate is not the same as the number that were actually translated.
        /// Indicates an internal ID translation failure.
        /// </summary>
        public const string IdTranslationResultCountMismatch = "IdTranslationResultCountMismatch";

        /// <summary>
        /// Returned when an identity read returned null after we attempted to create an identity ID translation,
        /// which indicates that the translation was not created successfully.
        /// </summary>
        public const string IdTranslationResultNull = "IdTranslationResultNull";

        /// <summary>
        /// Returned when we are trying to perform an operation on an unsupported host type.
        /// For example, AAD operations require an application host or lower and will return this error at the deployment level.
        /// </summary>
        public const string InvalidHostType = "InvalidHostType";

        /// <summary>
        /// Returned when the license string passed in by the caller is not a <see cref="DirectoryUserLicenseType"/>.
        /// </summary>
        public const string InvalidLicenseType = "InvalidLicenseType";

        /// Returned when the local descriptor passed in by the caller cannot be parsed as a valid identity descriptor.
        /// </summary>
        public const string InvalidLocalDescriptor = "InvalidLocalDescriptor";

        /// <summary>
        /// Returned when the permissions object passed in by the caller does not match the expected object structure.
        /// </summary>
        public const string InvalidPermissionsProperty = "InvalidPermissionsProperty";

        /// <summary>
        /// Returned when the profile string passed in by the caller is not a <see cref="DirectoryUserProfileState"/>.
        /// </summary>
        public const string InvalidProfileState = "InvalidProfileState";

        /// <summary>
        /// Returned when the request entity passed in by the caller cannot be parsed as some known implementation of IDirectoryEntityDescriptor.
        /// </summary>
        public const string InvalidRequestEntity = "InvalidRequestEntity";

        /// <summary>
        /// Returned when no licenses of the requested type are available,
        /// such as when the backing account has already used up its quota of purchased licenses.
        /// </summary>
        public const string LicenseNotAvailable = "LicenseNotAvailable";

        /// <summary>
        /// Returned when we failed to assign a license because we are targetting an organization host that has multiple or no child collection hosts.
        /// </summary>
        public const string LicenseTargetHostAmbiguous = "LicenseTargetHostAmbiguous";

        /// <summary>
        /// Returned when we failed to assign a license for a reason other than <see cref="LicenseNotAvailable"/>.
        /// </summary>
        public const string LicenseWriteFailed = "LicenseWriteFailed";

        /// <summary>
        /// Returned when we failed to assign a local group membership.
        /// </summary>
        public const string LocalGroupWriteFailed = "LocalGroupWriteFailed";

        /// <summary>
        /// Returned when we failed to read MSA users
        /// because we couldn't communicate with MSA or because MSA returned an invalid response,
        /// i.e. when MsaUserService calls fail.
        /// </summary>
        public const string MsaReadFailed = "MsaReadFailed";

        /// <summary>
        /// Returned when there was no result matching the request parameters across all backing directories.
        /// </summary>
        public const string NoResults = "NoResults";

        /// <summary>
        /// Returned when the requested entity is not effectively in scope,
        /// i.e. does not have an ancestor which is already a member of the account.
        /// </summary>
        public const string NotInScope = "NotInScope";

        /// <summary>
        /// Returned when one of the security namespace IDs passed in the permissions property
        /// cannot be resolved to a security namespace.
        /// </summary>
        public const string PermissionsNamespaceNotFound = "PermissionsNamespaceNotFound";

        /// <summary>
        /// Returned when we failed to set access control entries.
        /// </summary>
        public const string PermissionsWriteFailed = "PermissionsWriteFailed";

        /// <summary>
        /// Returned when we failed to create a profile.
        /// </summary>
        public const string ProfileWriteFailed = "ProfileWriteFailed";

        /// <summary>
        /// Returned when the request properties conflict with but do not exactly match a protected identity, for which an exact match is required.
        /// </summary>
        public const string ProtectedIdentityConflict = "ProtectedIdentityConflict";

        /// <summary>
        /// Returned when the number of results returned by a directory such as VSD or AAD.
        /// does not match the number of request members that we sent to that directory.
        /// </summary>
        public const string ResultCountMismatch = "ResultCountMismatch";

        /// <summary>
        /// Returned when we received a result but hit an error when trying to map that result back to the original request entity.
        /// </summary>
        public const string ResultMapFailed = "ResultMapFailed";

        /// <summary>
        /// Returned when we cannot root identities to make sure they are visible, even if only as inactive, in the target scope.
        /// </summary>
        public const string RootIdentitiesFailed = "RootIdentitiesFailed";

        /// <summary>
        /// Returned when we cannot verify whether the requested entity is effectively in scope.
        /// </summary>
        public const string ScopeNotDeterminable = "ScopeNotDeterminable";

        /// <summary>
        /// Returned when the request member was successfully discovered and added to the current scope
        /// with the request profile, license, and group memberships.
        /// </summary>
        public const string Success = "Success";

        /// <summary>
        /// Returned when we expected a single result from a backing directory such as VSD or AAD,
        /// but we received more than one result.
        /// Can occur when performing operations using potentially non-unique keys such as the principal name.
        /// Should never occur which using unique keys like the VSID or object ID.
        /// </summary>
        public const string TooManyResults = "TooManyResults";

        /// <summary>
        /// Returned when we failed to read VSD identities
        /// because we couldn't communicate with VSD or because VSD returned an invalid response,
        /// i.e. when IdentityService.ReadIdentities fails.
        /// </summary>
        public const string VsdReadFailed = "VsdReadFailed";

        /// <summary>
        /// Returned when we failed to write VSD identities
        /// because we couldn't communited with VSD or because VSD returned an invalid response
        /// i.e. when IdentityService.UpdateIdentities fails.
        /// </summary>
        public const string VsdWriteFailed = "VsdWriteFailed";

        /// <summary>
        /// Returned when we failed to create users in user service
        /// </summary>
        public const string UserServiceWriteFailed = "UserServiceWriteFailed";

        /// <summary>
        /// Returned when we failed to read GitHub objects
        /// because we couldn't communicate with GitHub or because GitHub returned an invalid response,
        /// i.e. when GitHubHttpClient.GetAnyUserById fails.
        /// </summary>
        public const string GitHubReadFailed = "GitHubReadFailed";
    }
}