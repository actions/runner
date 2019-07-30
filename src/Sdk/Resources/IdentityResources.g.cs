using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class IdentityResources
    {

        public static string FieldReadOnly(object arg0)
        {
            const string Format = @"{0} is read-only.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string GROUPCREATIONERROR(object arg0, object arg1)
        {
            const string Format = @"A group named {0} already exists in scope {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ADDMEMBERCYCLICMEMBERSHIPERROR(object arg0, object arg1)
        {
            const string Format = @"A cyclic group containment error occurred when adding a group member. The group {1} already has the group {0} as a contained member.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string GROUPSCOPECREATIONERROR(object arg0)
        {
            const string Format = @"The group scope {0} already exists";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ADDMEMBERIDENTITYALREADYMEMBERERROR(object arg0, object arg1)
        {
            const string Format = @"The group {0} already has a member {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string REMOVEGROUPMEMBERNOTMEMBERERROR(object arg0)
        {
            const string Format = @"An error occurred removing the group member. There is no group member with the security identifier (SID) {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string REMOVEADMINGROUPERROR()
        {
            const string Format = @"This group cannot be removed. The existence of this Administrators group is required.";
            return Format;
        }

        public static string REMOVEEVERYONEGROUPERROR()
        {
            const string Format = @"This group cannot be removed. The existence of this Valid Users group is required.";
            return Format;
        }

        public static string REMOVESERVICEGROUPERROR()
        {
            const string Format = @"This group cannot be removed. The existence of this Service Accounts group is required.";
            return Format;
        }

        public static string REMOVESPECIALGROUPERROR()
        {
            const string Format = @"This group cannot be removed. The existence of this group is required.";
            return Format;
        }

        public static string FINDGROUPSIDDOESNOTEXISTERROR(object arg0)
        {
            const string Format = @"An error occurred finding the group. There is no group with the security identifier (SID) {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string GROUPRENAMEERROR(object arg0)
        {
            const string Format = @"Error renaming group, a group named {0} already exists.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string GROUPSCOPEDOESNOTEXISTERROR(object arg0)
        {
            const string Format = @"The identity scope {0} does not exist";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityNotFoundMessage(object arg0)
        {
            const string Format = @"The identity with type '{0}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityNotFoundWithDescriptor(object arg0, object arg1)
        {
            const string Format = @"The identity with type '{0}' and identifier '{1}' could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string IdentityNotFoundSimpleMessage()
        {
            const string Format = @"The identity could not be found.";
            return Format;
        }

        public static string IdentityNotFoundWithTfid(object arg0)
        {
            const string Format = @"The identity with TeamFoundationId {0} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityNotFoundWithName(object arg0)
        {
            const string Format = @"The identity with name {0} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityAccountNameAlreadyInUseError(object arg0)
        {
            const string Format = @"The identity account name '{0}' is already in use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityAccountNameCollisionRepairFailedError(object arg0)
        {
            const string Format = @"Support will be required to repair this account. An attempt to repair an account name collision for identity '{0}' failed and cannot be completed automatically.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityAccountNameCollisionRepairUnsafeError(object arg0)
        {
            const string Format = @"Support will be required to repair this account. An attempt to repair an account name collision for identity '{0}' is unsafe and cannot be completed automatically.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityAliasAlreadyInUseError(object arg0)
        {
            const string Format = @"The identity alias '{0}' is already in use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidNameNotRecognized(object arg0)
        {
            const string Format = @"You have specified a name, {0}, that contains character(s) that are not recognized. Specify a name that only contains characters that are supported by the database collation setting and try again.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityMapReadOnlyException()
        {
            const string Format = @"The identity map cannot be accessed while the collection is detached.";
            return Format;
        }

        public static string IdentityAccountNamesAlreadyInUseError(object arg0, object arg1)
        {
            const string Format = @"{0} identity account names including '{1}' are already in use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidServiceIdentityName(object arg0)
        {
            const string Format = @"Service identities are limited to a maximum of 200 characters, and may only contain alpha numeric, dash, and space characters. The name '{0}' is not a valid service identity name.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountPreferencesAlreadyExist()
        {
            const string Format = @"Organization preferences have already been set. You can only set the preferences for language, culture, and time zone when the organization is created, and these preferences cannot be changed.";
            return Format;
        }

        public static string ADDGROUPMEMBERILLEGALINTERNETIDENTITY(object arg0)
        {
            const string Format = @"Internet identities cannot be added to this server. Unable to add {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ADDGROUPMEMBERILLEGALWINDOWSIDENTITY(object arg0)
        {
            const string Format = @"Windows users cannot be added to this server. Unable to add {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ADDPROJECTGROUPTPROJECTMISMATCHERROR(object arg0, object arg1)
        {
            const string Format = @"Project group '{1}' cannot be added to group '{0}', it is from a different project.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string CANNOT_REMOVE_SERVICE_ACCOUNT()
        {
            const string Format = @"You cannot remove the service account from the Service Accounts group.";
            return Format;
        }

        public static string IDENTITYDOMAINDOESNOTEXISTERROR(object arg0)
        {
            const string Format = @"No identity domain exists with the following security identifier (SID): {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IDENTITYDOMAINMISMATCHERROR(object arg0, object arg1)
        {
            const string Format = @"The group that you wish to manage is not owned by service host {0}, it is owned by {1}. Please target your request at the correct host.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string IdentityProviderUnavailable(object arg0, object arg1)
        {
            const string Format = @"The identity provider for type {0}, identifier {1} is unavailable.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string IDENTITY_SYNC_ERROR(object arg0)
        {
            const string Format = @"Sync error for identity: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IllegalIdentityException(object arg0)
        {
            const string Format = @"The user or group name {0} contains unsupported characters, is empty, or too long.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MODIFYEVERYONEGROUPEXCEPTION()
        {
            const string Format = @"The Valid Users group cannot be modified directly.";
            return Format;
        }

        public static string NOT_APPLICATION_GROUP()
        {
            const string Format = @"The identity you are attempting to edit is not an application group.";
            return Format;
        }

        public static string NOT_A_SECURITY_GROUP(object arg0)
        {
            const string Format = @"The group {0} is not a security group and cannot be added to Server.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string REMOVENONEXISTENTGROUPERROR(object arg0)
        {
            const string Format = @"An error occurred removing the group. There is no group with the security identifier (SID) {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string RemoveSelfFromAdminGroupError(object arg0)
        {
            const string Format = @"You cannot remove yourself from the Administrators group. This is a safeguard to prevent an enterprise locking themselves out of a deployment or project collection. Please have another administrator remove your membership. Alternatively you can disable the safeguard by setting {0} to false in the TF registry.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ADDPROJECTGROUPTOGLOBALGROUPERROR(object arg0, object arg1)
        {
            const string Format = @"You cannot add the project group {0} to the global group {1}. ";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string DynamicIdentityTypeCreationNotSupported()
        {
            const string Format = @"Dynamic creation of identity types is no longer supported. Please check that the type of the identity you are trying to create is supported. ";
            return Format;
        }

        public static string TooManyResultsError()
        {
            const string Format = @"The query was aborted because it returned too many results. Please apply additional filters to reduce the size of the resultset returned.";
            return Format;
        }

        public static string IncompatibleScopeError(object arg0, object arg1)
        {
            const string Format = @"Group cannot be created in the requested scope {1} since the requested scope is not within the root scope {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidIdentityIdTranslations()
        {
            const string Format = @"New translations have a record that may corrupt the existing translation data.";
            return Format;
        }

        public static string MultipleIdentitiesFoundError(object arg0, object arg1)
        {
            const string Format = @"Multiple identities found matching '{0}'. Please specify one of the following identities:

{1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string IdentityIdTranslationsAreMigrated()
        {
            const string Format = @"Identity id translations are migrated to collection partition.";
            return Format;
        }

        public static string InvalidGetDescriptorRequestWithLocalId(object arg0)
        {
            const string Format = @"Input parameter '{0}' is not a valid local id.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityMaterializationFailedMessage(object arg0)
        {
            const string Format = @"Could not add user '{0}' at this time.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityDescriptorNotFoundWithMasterId(object arg0)
        {
            const string Format = @"Identity descriptor for master id '{0}' not found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IdentityDescriptorNotFoundWithLocalId(object arg0)
        {
            const string Format = @"Identity descriptor for local id '{0}' not found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string TooManyRequestedItemsError()
        {
            const string Format = @"The request was aborted because it contained too many requested items.";
            return Format;
        }

        public static string TooManyRequestedItemsErrorWithCount(object arg0, object arg1)
        {
            const string Format = @"The request was aborted because it contained too many requested items {0}, maximum allowed is {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidIdentityKeyMaps()
        {
            const string Format = @"New identity key maps have a record that may corrupt the existing key map data.";
            return Format;
        }

        public static string InvitationPendingMessage(object arg0, object arg1)
        {
            const string Format = @"{0} has not accepted the invitation to the {1} organization.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ShouldBePersonalAccountMessage()
        {
            const string Format = @"Your work or school account does not have access to this resource, but your personal account does.";
            return Format;
        }

        public static string ShouldCreatePersonalAccountMessage()
        {
            const string Format = @"The account you are trying to access only allows Microsoft Accounts. Please create a Microsoft Account with a different email address and ask your administrator to invite the new Microsoft Account.";
            return Format;
        }

        public static string ShouldBeWorkAccountMessage()
        {
            const string Format = @"Your personal account does not have access to this resource, but your work or school account does.";
            return Format;
        }

        public static string IdentityNotFoundInCurrentDirectory()
        {
            const string Format = @"The identity could not be found in the current directory.";
            return Format;
        }

        public static string InvalidIdentityIdException(object arg0)
        {
            const string Format = @"The identity ID is invalid for identity: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidIdentityDescriptorException(object arg0)
        {
            const string Format = @"The identity descriptor is invalid for identity: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string RestoreGroupScopeValidationError(object arg0)
        {
            const string Format = @"Restore group scope validation error: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AccountOwnerCannotBeRemovedFromGroup(object arg0)
        {
            const string Format = @"Current account owner is not allowed to be removed from {0} group. Please change the account owner and try again.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ProjectCollectionAdministrators()
        {
            const string Format = @"Project Collection Administrators";
            return Format;
        }
    }
}
