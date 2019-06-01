using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class IdentityResources
    {
        public static string FieldReadOnly(params object[] args)
        {
            const string Format = @"{0} is read-only.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GROUPCREATIONERROR(params object[] args)
        {
            const string Format = @"TF50624: A group named {0} already exists in scope {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ADDMEMBERCYCLICMEMBERSHIPERROR(params object[] args)
        {
            const string Format = @"TF50233: A cyclic group containment error occurred when adding a group member. The group {1} already has the group {0} as a contained member.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GROUPSCOPECREATIONERROR(params object[] args)
        {
            const string Format = @"TF50620: The Azure DevOps group scope {0} already exists";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ADDMEMBERIDENTITYALREADYMEMBERERROR(params object[] args)
        {
            const string Format = @"TF50235: The group {0} already has a member {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string REMOVEGROUPMEMBERNOTMEMBERERROR(params object[] args)
        {
            const string Format = @"TF50632: An error occurred removing the group member. There is no group member with the security identifier (SID) {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string REMOVEADMINGROUPERROR(params object[] args)
        {
            const string Format = @"TF50633: This group cannot be removed. Azure DevOps requires the existence of this Administrators group for its operation.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string REMOVEEVERYONEGROUPERROR(params object[] args)
        {
            const string Format = @"TF50634: This group cannot be removed. Azure DevOps requires the existence of this Valid Users group for its operation.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string REMOVESERVICEGROUPERROR(params object[] args)
        {
            const string Format = @"TF50635: This group cannot be removed. Azure DevOps requires the existence of this Service Accounts group for its operation.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string REMOVESPECIALGROUPERROR(params object[] args)
        {
            const string Format = @"TF50636: This group cannot be removed. Azure DevOps requires the existence of this group for its operation.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string FINDGROUPSIDDOESNOTEXISTERROR(params object[] args)
        {
            const string Format = @"TF50258: An error occurred finding the group. There is no group with the security identifier (SID) {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GROUPRENAMEERROR(params object[] args)
        {
            const string Format = @"TF50616: Error renaming group, a group named {0} already exists.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string GROUPSCOPEDOESNOTEXISTERROR(params object[] args)
        {
            const string Format = @"TF50620: The Azure DevOps identity scope {0} does not exist";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityNotFoundMessage(params object[] args)
        {
            const string Format = @"TF14045: The identity with type '{0}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityNotFoundWithDescriptor(params object[] args)
        {
            const string Format = @"TF14045: The identity with type '{0}' and identifier '{1}' could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityNotFoundSimpleMessage(params object[] args)
        {
            const string Format = @"TF14045: The identity could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityNotFoundWithTfid(params object[] args)
        {
            const string Format = @"TF14045: The identity with TeamFoundationId {0} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityNotFoundWithName(params object[] args)
        {
            const string Format = @"TF14045: The identity with name {0} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityAccountNameAlreadyInUseError(params object[] args)
        {
            const string Format = @"TF400815: The identity account name '{0}' is already in use.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityAccountNameCollisionRepairFailedError(params object[] args)
        {
            const string Format = @"TF402001: Support will be required to repair this account. An attempt to repair an account name collision for identity '{0}' failed and cannot be completed automatically.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityAccountNameCollisionRepairUnsafeError(params object[] args)
        {
            const string Format = @"TF402002: Support will be required to repair this account. An attempt to repair an account name collision for identity '{0}' is unsafe and cannot be completed automatically.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityAliasAlreadyInUseError(params object[] args)
        {
            const string Format = @"The identity alias '{0}' is already in use.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidNameNotRecognized(params object[] args)
        {
            const string Format = @"TF200041: You have specified a name, {0}, that contains character(s) that are not recognized. Specify a name that only contains characters that are supported by the database collation setting and try again.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityMapReadOnlyException(params object[] args)
        {
            const string Format = @"TF401012: The identity map cannot be accessed while the collection is detached.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityAccountNamesAlreadyInUseError(params object[] args)
        {
            const string Format = @"TF400816: {0} identity account names including '{1}' are already in use.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidServiceIdentityName(params object[] args)
        {
            const string Format = @"TF400325: Service identities are limited to a maximum of 200 characters, and may only contain alpha numeric, dash, and space characters. The name '{0}' is not a valid service identity name.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountPreferencesAlreadyExist(params object[] args)
        {
            const string Format = @"TF400843: Organization preferences have already been set. You can only set the preferences for language, culture, and time zone when the organization is created, and these preferences cannot be changed.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ADDGROUPMEMBERILLEGALINTERNETIDENTITY(params object[] args)
        {
            const string Format = @"TF400448: Internet identities cannot be added to this server. Unable to add {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ADDGROUPMEMBERILLEGALWINDOWSIDENTITY(params object[] args)
        {
            const string Format = @"TF400447: Windows users cannot be added to this server. Unable to add {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ADDPROJECTGROUPTPROJECTMISMATCHERROR(params object[] args)
        {
            const string Format = @"TF50375: Project group '{1}' cannot be added to group '{0}', it is from a different project.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CANNOT_REMOVE_SERVICE_ACCOUNT(params object[] args)
        {
            const string Format = @"TF50248: You cannot remove the service account from the Service Accounts group.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IDENTITYDOMAINDOESNOTEXISTERROR(params object[] args)
        {
            const string Format = @"TF246076: No Azure DevOps identity domain exists with the following security identifier (SID): {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IDENTITYDOMAINMISMATCHERROR(params object[] args)
        {
            const string Format = @"TF50621: The Azure DevOps group that you wish to manage is not owned by service host {0}, it is owned by {1}. Please target your request at the correct host.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityProviderUnavailable(params object[] args)
        {
            const string Format = @"TF246104: The identity provider for type {0}, identifier {1} is unavailable.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IDENTITY_SYNC_ERROR(params object[] args)
        {
            const string Format = @"Sync error for identity: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IllegalIdentityException(params object[] args)
        {
            const string Format = @"TF10158: The user or group name {0} contains unsupported characters, is empty, or too long.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MODIFYEVERYONEGROUPEXCEPTION(params object[] args)
        {
            const string Format = @"TF50618: The Azure DevOps Valid Users group cannot be modified directly.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NOT_APPLICATION_GROUP(params object[] args)
        {
            const string Format = @"TF56044: The identity you are attempting to edit is not an application group.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NOT_A_SECURITY_GROUP(params object[] args)
        {
            const string Format = @"TF50619: The group {0} is not a security group and cannot be added to Azure DevOps Server.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string REMOVENONEXISTENTGROUPERROR(params object[] args)
        {
            const string Format = @"TF50265: An error occurred removing the group. There is no group with the security identifier (SID) {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RemoveSelfFromAdminGroupError(params object[] args)
        {
            const string Format = @"TF400571: You cannot remove yourself from the Administrators group. This is a safeguard to prevent an enterprise locking themselves out of a deployment or project collection. Please have another administrator remove your membership. Alternatively you can disable the safeguard by setting {0} to false in the TF registry.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ADDPROJECTGROUPTOGLOBALGROUPERROR(params object[] args)
        {
            const string Format = @"TF400031: You cannot add the project group {0} to the global group {1}. ";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DynamicIdentityTypeCreationNotSupported(params object[] args)
        {
            const string Format = @"TF50645: Dynamic creation of identity types is no longer supported. Please check that the type of the identity you are trying to create is supported. ";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TooManyResultsError(params object[] args)
        {
            const string Format = @"TF400048: The query was aborted because it returned too many results. Please apply additional filters to reduce the size of the resultset returned.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IncompatibleScopeError(params object[] args)
        {
            const string Format = @"TF400049: Group cannot be created in the requested scope {1} since the requested scope is not within the root scope {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidIdentityIdTranslations(params object[] args)
        {
            const string Format = @"VS401248: New translations have a record that may corrupt the existing translation data.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MultipleIdentitiesFoundError(params object[] args)
        {
            const string Format = @"Multiple identities found matching '{0}'. Please specify one of the following identities:

{1}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityIdTranslationsAreMigrated(params object[] args)
        {
            const string Format = @"Identity id translations are migrated to collection partition.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidGetDescriptorRequestWithLocalId(params object[] args)
        {
            const string Format = @"Input parameter '{0}' is not a valid local id.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityMaterializationFailedMessage(params object[] args)
        {
            const string Format = @"VS403283: Could not add user '{0}' at this time.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityDescriptorNotFoundWithMasterId(params object[] args)
        {
            const string Format = @"Identity descriptor for master id '{0}' not found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityDescriptorNotFoundWithLocalId(params object[] args)
        {
            const string Format = @"Identity descriptor for local id '{0}' not found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TooManyRequestedItemsError(params object[] args)
        {
            const string Format = @"TF400049: The request was aborted because it contained too many requested items.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TooManyRequestedItemsErrorWithCount(params object[] args)
        {
            const string Format = @"TF400049: The request was aborted because it contained too many requested items {0}, maximum allowed is {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidIdentityKeyMaps(params object[] args)
        {
            const string Format = @"VS401249: New identity key maps have a record that may corrupt the existing key map data.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvitationPendingMessage(params object[] args)
        {
            const string Format = @"VS403318: {0} has not accepted the invitation to the {1} organization.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ShouldBePersonalAccountMessage(params object[] args)
        {
            const string Format = @"VS403362: Your work or school account does not have access to this resource, but your personal account does.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ShouldCreatePersonalAccountMessage(params object[] args)
        {
            const string Format = @"VS403408: The VSTS account you are trying to access only allows Microsoft Accounts. Please create a Microsoft Account with a different email address and ask your administrator to invite the new Microsoft Account.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ShouldBeWorkAccountMessage(params object[] args)
        {
            const string Format = @"VS403363: Your personal account does not have access to this resource, but your work or school account does.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IdentityNotFoundInCurrentDirectory(params object[] args)
        {
            const string Format = @"The identity could not be found in the current directory.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidIdentityIdException(params object[] args)
        {
            const string Format = @"The identity ID is invalid for identity: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidIdentityDescriptorException(params object[] args)
        {
            const string Format = @"The identity descriptor is invalid for identity: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RestoreGroupScopeValidationError(params object[] args)
        {
            const string Format = @"Restore group scope validation error: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AccountOwnerCannotBeRemovedFromGroup(params object[] args)
        {
            const string Format = @"Current account owner is not allowed to be removed from {0} group. Please change the account owner and try again.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ProjectCollectionAdministrators(params object[] args)
        {
            const string Format = @"Project Collection Administrators";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
