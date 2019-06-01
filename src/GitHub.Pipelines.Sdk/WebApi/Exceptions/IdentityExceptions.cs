using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Identity
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityServiceException", "Microsoft.VisualStudio.Services.Identity.IdentityServiceException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityServiceException : VssServiceException
    {
        public IdentityServiceException()
        {
            EventId = VssEventId.VssIdentityServiceException;
        }

        public IdentityServiceException(string message)
            : base(message)
        {
            EventId = VssEventId.VssIdentityServiceException;
        }

        public IdentityServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
            EventId = VssEventId.VssIdentityServiceException;
        }

        protected IdentityServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            EventId = VssEventId.VssIdentityServiceException;
        }
    }
    /// <summary>
    /// The group you are creating already exists, thrown by the data tier
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "GroupCreationException", "Microsoft.VisualStudio.Services.Identity.GroupCreationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class GroupCreationException : IdentityServiceException
    {
        public GroupCreationException(string displayName, string projectName)
            : base(IdentityResources.GROUPCREATIONERROR(displayName, projectName))
        {
        }

        public GroupCreationException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// IMS domain is incorrect for operation
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityDomainMismatchException", "Microsoft.VisualStudio.Services.Identity.IdentityDomainMismatchException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityDomainMismatchException : IdentityServiceException
    {
        public IdentityDomainMismatchException(string incorrectHost, string correctHost)
            : base(IdentityResources.IDENTITYDOMAINMISMATCHERROR(incorrectHost, correctHost))
        {
        }

        public IdentityDomainMismatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected IdentityDomainMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// You are trying to add a group that is a parent group of the current group, throw
    /// by the data tier
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AddMemberCyclicMembershipException", "Microsoft.VisualStudio.Services.Identity.AddMemberCyclicMembershipException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddMemberCyclicMembershipException : IdentityServiceException
    {
        public AddMemberCyclicMembershipException()
        {
        }

        public AddMemberCyclicMembershipException(string groupName, string memberName)
            : base(IdentityResources.ADDMEMBERCYCLICMEMBERSHIPERROR(groupName, memberName))
        {
        }

        public AddMemberCyclicMembershipException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AddMemberCyclicMembershipException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// You are trying to create a group scope that already exists
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "GroupScopeCreationException", "Microsoft.VisualStudio.Services.Identity.GroupScopeCreationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class GroupScopeCreationException : IdentityServiceException
    {
        public GroupScopeCreationException()
        {
        }

        public GroupScopeCreationException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public GroupScopeCreationException(string scopeId)
            : base(IdentityResources.GROUPSCOPECREATIONERROR(scopeId))
        {
        }

        protected GroupScopeCreationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Group cannot be created in the requested scope since the requested scope is not within the root scope.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class IncompatibleScopeException : IdentityServiceException
    {
        public IncompatibleScopeException()
        {
        }

        public IncompatibleScopeException(String message): base(message)
        {
        }
        public IncompatibleScopeException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public IncompatibleScopeException(string rootScopeId, string scopeIdToCheck)
            : base(IdentityResources.IncompatibleScopeError(rootScopeId, scopeIdToCheck))
        {
        }

        protected IncompatibleScopeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Trying to add a member to a group that is already a member of the group, thrown by the data tier.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AddMemberIdentityAlreadyMemberException", "Microsoft.VisualStudio.Services.Identity.AddMemberIdentityAlreadyMemberException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddMemberIdentityAlreadyMemberException : IdentityServiceException
    {
        public AddMemberIdentityAlreadyMemberException(string groupName, string memberName)
            : base(IdentityResources.ADDMEMBERIDENTITYALREADYMEMBERERROR(groupName, memberName))
        {
        }

        public AddMemberIdentityAlreadyMemberException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AddMemberIdentityAlreadyMemberException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveAccountOwnerFromAdminGroupException", "Microsoft.VisualStudio.Services.Identity.RemoveAccountOwnerFromAdminGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RemoveAccountOwnerFromAdminGroupException : IdentityServiceException
    {      
        public RemoveAccountOwnerFromAdminGroupException() 
            : base(IdentityResources.AccountOwnerCannotBeRemovedFromGroup(IdentityResources.ProjectCollectionAdministrators())) { }

        public RemoveAccountOwnerFromAdminGroupException(string message) : base(message){ }

        public RemoveAccountOwnerFromAdminGroupException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// You can't remove yourself from the global namespace admins group and lock yourself out of your collection/hosting account.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveSelfFromAdminGroupException", "Microsoft.VisualStudio.Services.Identity.RemoveSelfFromAdminGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RemoveSelfFromAdminGroupException : IdentityServiceException
    {
        public RemoveSelfFromAdminGroupException()
            : base(IdentityResources.RemoveSelfFromAdminGroupError(BlockRemovingSelfFromAdminGroup))
        {
        }

        public RemoveSelfFromAdminGroupException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private const String BlockRemovingSelfFromAdminGroup = @"/Service/Integration/Settings/BlockRemovingSelfFromAdminGroup";
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveOrganizationAdminFromAdminGroupException", "Microsoft.VisualStudio.Services.Identity.RemoveOrganizationAdminFromAdminGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    class RemoveOrganizationAdminFromAdminGroupException : IdentityServiceException
    {
        public RemoveOrganizationAdminFromAdminGroupException(string message) : base(message) { }

        public RemoveOrganizationAdminFromAdminGroupException(String message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveServiceAccountsFromAdminGroupException", "Microsoft.VisualStudio.Services.Identity.RemoveServiceAccountsFromAdminGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    class RemoveServiceAccountsFromAdminGroupException : IdentityServiceException
    {
        public RemoveServiceAccountsFromAdminGroupException(string message) : base(message) { }
        public RemoveServiceAccountsFromAdminGroupException(String message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Group member you are trying to delete was not a member of the group.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveGroupMemberNotMemberException", "Microsoft.VisualStudio.Services.Identity.RemoveGroupMemberNotMemberException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RemoveGroupMemberNotMemberException : IdentityServiceException
    {
        public RemoveGroupMemberNotMemberException(string sid)
            : base(IdentityResources.REMOVEGROUPMEMBERNOTMEMBERERROR(sid))
        {
        }

        public RemoveGroupMemberNotMemberException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when an AddMemberToGroup call is made to put an identity X into group Y, but the action
    /// is not legal for some reason related to identity X
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AddGroupMemberIllegalMemberException", "Microsoft.VisualStudio.Services.Identity.AddGroupMemberIllegalMemberException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddGroupMemberIllegalMemberException : IdentityServiceException
    {
        public AddGroupMemberIllegalMemberException()
        {
        }

        public AddGroupMemberIllegalMemberException(String message)
            : base(message)
        {
        }

        public AddGroupMemberIllegalMemberException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AddGroupMemberIllegalMemberException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Cannot add windows identity to hosted deployment
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AddGroupMemberIllegalWindowsIdentityException", "Microsoft.VisualStudio.Services.Identity.AddGroupMemberIllegalWindowsIdentityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddGroupMemberIllegalWindowsIdentityException : IdentityServiceException
    {
        public AddGroupMemberIllegalWindowsIdentityException(Identity member)
            : base(IdentityResources.ADDGROUPMEMBERILLEGALWINDOWSIDENTITY(member.DisplayName))
        {
        }

        public AddGroupMemberIllegalWindowsIdentityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Cannot add internet identity to on premise deployment
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AddGroupMemberIllegalInternetIdentityException", "Microsoft.VisualStudio.Services.Identity.AddGroupMemberIllegalInternetIdentityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddGroupMemberIllegalInternetIdentityException : IdentityServiceException
    {
        public AddGroupMemberIllegalInternetIdentityException(Identity member)
            : base(IdentityResources.ADDGROUPMEMBERILLEGALINTERNETIDENTITY(member.DisplayName))
        {
        }

        public AddGroupMemberIllegalInternetIdentityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Trying to remove a group that doesn't exist, thrown by the data tier
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveNonexistentGroupException", "Microsoft.VisualStudio.Services.Identity.RemoveNonexistentGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RemoveNonexistentGroupException : IdentityServiceException
    {
        public RemoveNonexistentGroupException(string sid)
            : base(IdentityResources.REMOVENONEXISTENTGROUPERROR(sid))
        {
        }

        public RemoveNonexistentGroupException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// You can't remove any of the special groups: the global administrators group, the
    /// service users group, the team foundation valid users group, or a project administration
    /// group.  Thrown by the data tier.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveSpecialGroupException", "Microsoft.VisualStudio.Services.Identity.RemoveSpecialGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RemoveSpecialGroupException : IdentityServiceException
    {
        public RemoveSpecialGroupException(string sid, SpecialGroupType specialType)
            : base(BuildMessage(sid, specialType))
        {
        }

        public RemoveSpecialGroupException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected static string BuildMessage(string sid, SpecialGroupType specialType)
        {
            switch (specialType)
            {
                case SpecialGroupType.AdministrativeApplicationGroup:
                    return IdentityResources.REMOVEADMINGROUPERROR();

                case SpecialGroupType.EveryoneApplicationGroup:
                    return IdentityResources.REMOVEEVERYONEGROUPERROR();

                case SpecialGroupType.ServiceApplicationGroup:
                    return IdentityResources.REMOVESERVICEGROUPERROR();

                default:
                    return IdentityResources.REMOVESPECIALGROUPERROR();
            }
        }
    }

    /// <summary>
    /// Group you were looking up does not exist, thrown by the data tier
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "FindGroupSidDoesNotExistException", "Microsoft.VisualStudio.Services.Identity.FindGroupSidDoesNotExistException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class FindGroupSidDoesNotExistException : IdentityServiceException
    {
        public FindGroupSidDoesNotExistException(string sid)
            : base(IdentityResources.FINDGROUPSIDDOESNOTEXISTERROR(sid))
        {
        }

        public FindGroupSidDoesNotExistException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FindGroupSidDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Group rename error, new name already in use
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "GroupRenameException", "Microsoft.VisualStudio.Services.Identity.GroupRenameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class GroupRenameException : IdentityServiceException
    {
        public GroupRenameException(string displayName)
            : base(IdentityResources.GROUPRENAMEERROR(displayName))
        {
        }

        public GroupRenameException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// You cannot add a project group to a project group in a different project
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AddProjectGroupProjectMismatchException", "Microsoft.VisualStudio.Services.Identity.AddProjectGroupProjectMismatchException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddProjectGroupProjectMismatchException : IdentityServiceException
    {
        public AddProjectGroupProjectMismatchException()
        {
        }

        public AddProjectGroupProjectMismatchException(string groupName, string memberName)
            : base(IdentityResources.ADDPROJECTGROUPTPROJECTMISMATCHERROR(groupName, memberName))
        {
        }

        public AddProjectGroupProjectMismatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AddProjectGroupProjectMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AddProjectGroupToGlobalGroupException", "Microsoft.VisualStudio.Services.Identity.AddProjectGroupToGlobalGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AddProjectGroupToGlobalGroupException : IdentityServiceException
    {
        public AddProjectGroupToGlobalGroupException()
        {
        }

        public AddProjectGroupToGlobalGroupException(string globalGroupName, string projectGroupName)
            : base(IdentityResources.ADDPROJECTGROUPTOGLOBALGROUPERROR(projectGroupName, globalGroupName))
        {
        }

        public AddProjectGroupToGlobalGroupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AddProjectGroupToGlobalGroupException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Unable to locate project for the project uri passed in
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "GroupScopeDoesNotExistException", "Microsoft.VisualStudio.Services.Identity.GroupScopeDoesNotExistException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class GroupScopeDoesNotExistException : IdentityServiceException
    {
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        public GroupScopeDoesNotExistException(string projectUri)
            : base(IdentityResources.GROUPSCOPEDOESNOTEXISTERROR(projectUri))
        {
        }

        public GroupScopeDoesNotExistException(Guid scopeId)
            : base(IdentityResources.GROUPSCOPEDOESNOTEXISTERROR(scopeId))
        {
        }

        public GroupScopeDoesNotExistException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GroupScopeDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when a user tries to add a group that is
    /// not an application group.  We do not modify the memberships of Windows groups.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "NotApplicationGroupException", "Microsoft.VisualStudio.Services.Identity.NotApplicationGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class NotApplicationGroupException : IdentityServiceException
    {
        public NotApplicationGroupException()
            : base(IdentityResources.NOT_APPLICATION_GROUP())
        {
        }

        public NotApplicationGroupException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// You must specify a group when removing members from a group, thrown by the app tier
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ModifyEveryoneGroupException", "Microsoft.VisualStudio.Services.Identity.ModifyEveryoneGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ModifyEveryoneGroupException : IdentityServiceException
    {
        public ModifyEveryoneGroupException()
            : base(IdentityResources.MODIFYEVERYONEGROUPEXCEPTION())
        {
        }

        public ModifyEveryoneGroupException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// ReadIdentityFromSource returned null and we need an identity to continue the operation
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityNotFoundException", "Microsoft.VisualStudio.Services.Identity.IdentityNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityNotFoundException : IdentityServiceException
    {
        public IdentityNotFoundException()
            : base(IdentityResources.IdentityNotFoundSimpleMessage())
        {
        }

        public IdentityNotFoundException(String message)
            : base(message)
        {
        }

        public IdentityNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public IdentityNotFoundException(IdentityDescriptor descriptor)
            : base(IdentityResources.IdentityNotFoundMessage(descriptor.IdentityType))
        {
        }

        public IdentityNotFoundException(SubjectDescriptor subjectDescriptor)
            : base(IdentityResources.IdentityNotFoundMessage(subjectDescriptor.SubjectType))
        {
        }

        public IdentityNotFoundException(Guid tfid)
            : base(IdentityResources.IdentityNotFoundWithTfid(tfid))
        {
        }
    }

    /// <summary>
    /// Identity is not part of calling identity's directory 
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class IdentityNotFoundInCurrentDirectoryException : IdentityServiceException
    {
        public IdentityNotFoundInCurrentDirectoryException()
            : base(IdentityResources.IdentityNotFoundInCurrentDirectory())
        {
        }

        public IdentityNotFoundInCurrentDirectoryException(String message)
            : base(message)
        {
        }

        public IdentityNotFoundInCurrentDirectoryException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The identity is not a service identity
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityNotServiceIdentityException", "Microsoft.VisualStudio.Services.Identity.IdentityNotServiceIdentityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityNotServiceIdentityException : IdentityServiceException
    {
        public IdentityNotServiceIdentityException(String message)
            : base(message)
        {
        }

        public IdentityNotServiceIdentityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidServiceIdentityNameException", "Microsoft.VisualStudio.Services.Identity.InvalidServiceIdentityNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidServiceIdentityNameException : IdentityServiceException
    {
        public InvalidServiceIdentityNameException(String identityName)
            : base(IdentityResources.InvalidServiceIdentityName(identityName))
        {
        }

        public InvalidServiceIdentityNameException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The identity already exists
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityAlreadyExistsException", "Microsoft.VisualStudio.Services.Identity.IdentityAlreadyExistsException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityAlreadyExistsException : IdentityServiceException
    {
        public IdentityAlreadyExistsException(String message)
            : base(message)
        {
        }

        public IdentityAlreadyExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when a user tries to add a distribution list
    /// to a group.  We only allow security groups to used.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "NotASecurityGroupException", "Microsoft.VisualStudio.Services.Identity.NotASecurityGroupException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class NotASecurityGroupException : IdentityServiceException
    {
        public NotASecurityGroupException(String displayName)
            : base(IdentityResources.NOT_A_SECURITY_GROUP(displayName))
        {
        }

        public NotASecurityGroupException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RemoveMemberServiceAccountException", "Microsoft.VisualStudio.Services.Identity.RemoveMemberServiceAccountException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RemoveMemberServiceAccountException : IdentityServiceException
    {
        public RemoveMemberServiceAccountException()
            : base(IdentityResources.CANNOT_REMOVE_SERVICE_ACCOUNT())
        {
        }

        public RemoveMemberServiceAccountException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IllegalAliasException", "Microsoft.VisualStudio.Services.Identity.IllegalAliasException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IllegalAliasException : IdentityServiceException
    {
        public IllegalAliasException(string name) :
            base(name)
        {
        }

        public IllegalAliasException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IllegalIdentityException", "Microsoft.VisualStudio.Services.Identity.IllegalIdentityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IllegalIdentityException : IdentityServiceException
    {
        public IllegalIdentityException(string name) :
            base(IdentityResources.IllegalIdentityException(name))
        {
        }

        public IllegalIdentityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentitySyncException", "Microsoft.VisualStudio.Services.Identity.IdentitySyncException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentitySyncException : IdentityServiceException
    {
        public IdentitySyncException(string message, Exception innerException) :
            base(IdentityResources.IDENTITY_SYNC_ERROR(message))
        {
        }
    }

    /// <summary>
    /// Identity provider not available
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityProviderUnavailableException", "Microsoft.VisualStudio.Services.Identity.IdentityProviderUnavailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityProviderUnavailableException : IdentityServiceException
    {
        public IdentityProviderUnavailableException(IdentityDescriptor descriptor)
            : base(IdentityResources.IdentityProviderUnavailable(descriptor.IdentityType, descriptor.Identifier))
        {
        }

        public IdentityProviderUnavailableException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityPropertyRequiredException", "Microsoft.VisualStudio.Services.Identity.IdentityPropertyRequiredException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityPropertyRequiredException : IdentityServiceException
    {
        public IdentityPropertyRequiredException(String message)
            : base(message)
        {
        }

        public IdentityPropertyRequiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityAccountNameAlreadyInUseException", "Microsoft.VisualStudio.Services.Identity.IdentityAccountNameAlreadyInUseException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityAccountNameAlreadyInUseException : IdentityServiceException
    {
        public IdentityAccountNameAlreadyInUseException(String oneAccountName, Int32 collisionCount)
            : base(BuildExceptionMessage(oneAccountName, collisionCount))
        {
        }

        public IdentityAccountNameAlreadyInUseException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private static String BuildExceptionMessage(String oneAccountName, Int32 collisionCount)
        {
            Debug.Assert(collisionCount > 0, "identity account name exception fired, but no collisions were found");

            if (collisionCount == 1)
            {
                return IdentityResources.IdentityAccountNameAlreadyInUseError(oneAccountName);
            }
            else
            {
                return IdentityResources.IdentityAccountNamesAlreadyInUseError(collisionCount, oneAccountName);
            }
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityAccountNameCollisionRepairFailedException", "Microsoft.VisualStudio.Services.Identity.IdentityAccountNameCollisionRepairFailedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityAccountNameCollisionRepairFailedException : IdentityServiceException
    {
        public IdentityAccountNameCollisionRepairFailedException(String accountName)
            : base(IdentityResources.IdentityAccountNameCollisionRepairFailedError(accountName))
        {
        }

        public IdentityAccountNameCollisionRepairFailedException(String accountName, Exception innerException)
            : base(IdentityResources.IdentityAccountNameCollisionRepairFailedError(accountName), innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityAccountNameCollisionRepairUnsafeException", "Microsoft.VisualStudio.Services.Identity.IdentityAccountNameCollisionRepairUnsafeException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityAccountNameCollisionRepairUnsafeException : IdentityServiceException
    {
        public IdentityAccountNameCollisionRepairUnsafeException(String accountName)
            : base(IdentityResources.IdentityAccountNameCollisionRepairUnsafeError(accountName))
        {
        }

        public IdentityAccountNameCollisionRepairUnsafeException(String accountName, Exception innerException)
            : base(IdentityResources.IdentityAccountNameCollisionRepairUnsafeError(accountName), innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityAliasAlreadyInUseException", "Microsoft.VisualStudio.Services.Identity.IdentityAliasAlreadyInUseException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityAliasAlreadyInUseException : IdentityServiceException
    {
        public IdentityAliasAlreadyInUseException(String conflictingAlias)
            : base(IdentityResources.IdentityAliasAlreadyInUseError(conflictingAlias))
        {
        }

        public IdentityAliasAlreadyInUseException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "DynamicIdentityTypeCreationNotSupportedException", "Microsoft.VisualStudio.Services.Identity.DynamicIdentityTypeCreationNotSupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DynamicIdentityTypeCreationNotSupportedException : IdentityServiceException
    {
        public DynamicIdentityTypeCreationNotSupportedException()
            : base(IdentityResources.DynamicIdentityTypeCreationNotSupported())
        {
        }

        public DynamicIdentityTypeCreationNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "TooManyIdentitiesReturnedException", "Microsoft.VisualStudio.Services.Identity.TooManyIdentitiesReturnedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TooManyIdentitiesReturnedException : IdentityServiceException
    {
        public TooManyIdentitiesReturnedException()
            : base(IdentityResources.TooManyResultsError())
        {
        }

        public TooManyIdentitiesReturnedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "MultipleIdentitiesFoundException", "Microsoft.VisualStudio.Services.Identity.MultipleIdentitiesFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MultipleIdentitiesFoundException : IdentityServiceException
    {
        public MultipleIdentitiesFoundException(string identityName, IEnumerable<Identity> matchingIdentities)
            : base(BuildExceptionMessage(identityName, matchingIdentities))
        {

        }

        public MultipleIdentitiesFoundException(string identityName, IEnumerable<IReadOnlyVssIdentity> matchingIdentities)
            : base(BuildExceptionMessage(identityName, matchingIdentities))
        {

        }

        public MultipleIdentitiesFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private static string BuildExceptionMessage(string identityName, IEnumerable<IReadOnlyVssIdentity> matchingIdentities)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var identity in matchingIdentities)
            {
                builder.AppendFormat(CultureInfo.CurrentUICulture, "- {0} ({1})", identity.ProviderDisplayName, identity.CustomDisplayName);
            }

            return IdentityResources.MultipleIdentitiesFoundError(identityName, builder.ToString());
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "HistoricalIdentityNotFoundException", "Microsoft.VisualStudio.Services.Identity.HistoricalIdentityNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class HistoricalIdentityNotFoundException : IdentityServiceException
    {
        public HistoricalIdentityNotFoundException()
            : base(IdentityResources.TooManyResultsError())
        {
        }

        public HistoricalIdentityNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidIdentityIdTranslationException", "Microsoft.VisualStudio.Services.Identity.InvalidIdentityIdTranslationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidIdentityIdTranslationException : IdentityServiceException
    {
        public InvalidIdentityIdTranslationException()
            : base(IdentityResources.InvalidIdentityIdTranslations())
        {
        }

        public InvalidIdentityIdTranslationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdTranslationsAreMigratedException", "Microsoft.VisualStudio.Services.Identity.IdTranslationsAreMigratedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdTranslationsAreMigratedException : IdentityServiceException
    {
        public IdTranslationsAreMigratedException()
            : base(IdentityResources.IdentityIdTranslationsAreMigrated())
        {
        }

        public IdTranslationsAreMigratedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidIdentityStorageKeyTranslationException", "Microsoft.VisualStudio.Services.Identity.InvalidIdentityStorageKeyTranslationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidIdentityStorageKeyTranslationException : IdentityServiceException
    {
        public InvalidIdentityStorageKeyTranslationException()
            : base(IdentityResources.InvalidIdentityKeyMaps())
        {
        }

        public InvalidIdentityStorageKeyTranslationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidIdentityKeyMapsException", "Microsoft.VisualStudio.Services.Identity.InvalidIdentityKeyMapsException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidIdentityKeyMapException : IdentityServiceException
    {
        public InvalidIdentityKeyMapException()
            : base(IdentityResources.InvalidIdentityKeyMaps())
        {
        }

        public InvalidIdentityKeyMapException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidTypeIdForIdentityStorageKeyException", "Microsoft.VisualStudio.Services.Identity.InvalidTypeIdForIdentityStorageKeyException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidTypeIdForIdentityKeyMapException : IdentityServiceException
    {
        public InvalidTypeIdForIdentityKeyMapException()
            : base(IdentityResources.InvalidIdentityKeyMaps())
        {
        }

        public InvalidTypeIdForIdentityKeyMapException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "DuplicateIdentitiesFoundException", "Microsoft.VisualStudio.Services.Identity.DuplicateIdentitiesFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DuplicateIdentitiesFoundException : IdentityServiceException
    {
        public DuplicateIdentitiesFoundException()
            : base(IdentityResources.InvalidIdentityIdTranslations())
        {
        }

        public DuplicateIdentitiesFoundException(String message)
            : base(message)
        {
        }

        public DuplicateIdentitiesFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityExpressionException", "Microsoft.VisualStudio.Services.Identity.IdentityExpressionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityExpressionException : IdentityServiceException
    {
        public IdentityExpressionException(String message)
            : base(message)
        {
        }

        public IdentityExpressionException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidDisplayNameException", "Microsoft.VisualStudio.Services.Identity.InvalidDisplayNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidDisplayNameException : IdentityServiceException
    {
        public InvalidDisplayNameException(String message)
            : base(message)
        {
        }

        public InvalidDisplayNameException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "GroupNameNotRecognizedException", "Microsoft.VisualStudio.Services.Identity.GroupNameNotRecognizedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class GroupNameNotRecognizedException : IdentityServiceException
    {
        public GroupNameNotRecognizedException()
        {
        }

        public GroupNameNotRecognizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public GroupNameNotRecognizedException(string groupName)
            : this(IdentityResources.InvalidNameNotRecognized(groupName), null)
        {
        }

        protected GroupNameNotRecognizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountPreferencesAlreadyExistException", "Microsoft.VisualStudio.Services.Identity.AccountPreferencesAlreadyExistException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountPreferencesAlreadyExistException : IdentityServiceException
    {
        public AccountPreferencesAlreadyExistException()
            : base(IdentityResources.AccountPreferencesAlreadyExist())
        {
        }

        public AccountPreferencesAlreadyExistException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMapReadOnlyException", "Microsoft.VisualStudio.Services.Identity.IdentityMapReadOnlyException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMapReadOnlyException : IdentityServiceException
    {
        public IdentityMapReadOnlyException()
            : this((Exception)null)
        {
        }

        public IdentityMapReadOnlyException(Exception innerException)
            : base(IdentityResources.IdentityMapReadOnlyException(), innerException)
        {
        }

        public IdentityMapReadOnlyException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected IdentityMapReadOnlyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityStoreNotAvailableException", "Microsoft.VisualStudio.Services.Identity.IdentityStoreNotAvailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityStoreNotAvailableException : IdentityServiceException
    {
        public IdentityStoreNotAvailableException() : base() { }
        public IdentityStoreNotAvailableException(string errorMessage) : base(errorMessage) { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidDisplayNameException", "Microsoft.VisualStudio.Services.Identity.InvalidChangedIdentityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidChangedIdentityException : IdentityServiceException
    {
        public InvalidChangedIdentityException(String message)
            : base(message)
        {
        }

        public InvalidChangedIdentityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdenittyInvalidTypeIdException", "Microsoft.VisualStudio.Services.Identity.IdenittyInvalidTypeIdException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [Obsolete("This exception has been renamed to IdentityInvalidTypeIdException")]
    public class IdenittyInvalidTypeIdException : IdentityServiceException
    {
        public IdenittyInvalidTypeIdException(string message) :
            base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "IdentityInvalidTypeIdException", "Microsoft.VisualStudio.Services.Identity.IdentityInvalidTypeIdException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#pragma warning disable 618
    public class IdentityInvalidTypeIdException : IdenittyInvalidTypeIdException
#pragma warning restore 618
    {
        public IdentityInvalidTypeIdException(string message) :
            base(message)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidIdentityKeyException", "Microsoft.VisualStudio.Services.Identity.InvalidIdentityKeyException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidIdentityKeyException : IdentityServiceException
    {
        public InvalidIdentityKeyException() : base() { }

        public InvalidIdentityKeyException(string message) :
            base(message)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "IdentityMaterializationFailedException", "Microsoft.VisualStudio.Services.Identity.IdentityMaterializationFailedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class IdentityMaterializationFailedException : IdentityServiceException
    {
        public IdentityMaterializationFailedException()
        {
        }

        public IdentityMaterializationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public IdentityMaterializationFailedException(string principalName)
            : this(IdentityResources.IdentityMaterializationFailedMessage(principalName), null)
        {
        }

        protected IdentityMaterializationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class IdentityDescriptorNotFoundException : IdentityServiceException
    {
        public IdentityDescriptorNotFoundException()
        { }

        public IdentityDescriptorNotFoundException(string message)
            : base(message)
        { }

        public IdentityDescriptorNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdentityDescriptorNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public IdentityDescriptorNotFoundException(Guid id, bool isMasterId)
            : base(isMasterId ?
                  IdentityResources.IdentityDescriptorNotFoundWithMasterId(id) :
                  IdentityResources.IdentityDescriptorNotFoundWithLocalId(id))
        {
        }
    }

    [Serializable]
    public abstract class TenantSwitchException : IdentityServiceException
    {
        public TenantSwitchException()
        {
        }

        public TenantSwitchException(string message) : base(message)
        {
        }

        public TenantSwitchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TenantSwitchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvitationPendingException : TenantSwitchException
    {
        public string AccountName { get; }
        public string OrganizationName { get; }

        public InvitationPendingException()
        {
        }

        public InvitationPendingException(string message)
            : base(message)
        {
        }

        public InvitationPendingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvitationPendingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InvitationPendingException(string accountName, string organizationName)
            : base(IdentityResources.InvitationPendingMessage(accountName, organizationName))
        {
            AccountName = accountName;
            OrganizationName = organizationName;
        }
    }

    [Serializable]
    public class WrongWorkOrPersonalException : TenantSwitchException
    {
        public string AccountName { get; }
        public bool ShouldBePersonal { get; }
        public bool ShouldCreatePersonal { get; }

        public WrongWorkOrPersonalException()
        {
        }

        public WrongWorkOrPersonalException(string message)
            : base(message)
        {
        }

        public WrongWorkOrPersonalException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WrongWorkOrPersonalException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public WrongWorkOrPersonalException(string accountName, bool shouldBePersonal, bool shouldCreatePersonal)
            : base(GetMessage(shouldBePersonal, shouldCreatePersonal))
        {
            AccountName = accountName;
            ShouldBePersonal = shouldBePersonal;
            ShouldCreatePersonal = shouldCreatePersonal;
        }

        private static string GetMessage(bool shouldBePersonal, bool shouldCreatePersonal)
        {
            if (shouldBePersonal)
            {
                if (shouldCreatePersonal)
                {
                    return IdentityResources.ShouldCreatePersonalAccountMessage();
                }
                else
                {
                    return IdentityResources.ShouldBePersonalAccountMessage();
                }
            }
            else
            {
                return IdentityResources.ShouldBeWorkAccountMessage();
            }
        }
    }

    [Serializable]
    public class InvalidTransferIdentityRightsRequestException : IdentityServiceException
    {
        public InvalidTransferIdentityRightsRequestException()
        {
        }

        public InvalidTransferIdentityRightsRequestException(string message)
            : base(message)
        {
        }

        public InvalidTransferIdentityRightsRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidTransferIdentityRightsRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class FailedTransferIdentityRightsException : IdentityServiceException
    {
        public FailedTransferIdentityRightsException()
        {
        }

        public FailedTransferIdentityRightsException(string message)
            : base(message)
        {
        }

        public FailedTransferIdentityRightsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FailedTransferIdentityRightsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionShardingException : IdentityServiceException
    {
        public CollectionShardingException()
        {
        }

        public CollectionShardingException(string message)
            : base(message)
        {
        }

        public CollectionShardingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionShardingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ScopeBadRequestException: IdentityServiceException
    {
        protected ScopeBadRequestException()
        {
        }

        public ScopeBadRequestException(string message)
            : base(message)
        {
        }

        public ScopeBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ScopeBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Indicates that a caller action triggered an attempt to read or update identity information at the deployment level
    /// directly from (or using data from) a sharded host after dual writes had been disabled, meaning that the fallback is not allowed.
    /// </summary>
    [Serializable]
    public class FallbackIdentityOperationNotAllowedException : IdentityServiceException
    {
        public FallbackIdentityOperationNotAllowedException()
        {
        }

        public FallbackIdentityOperationNotAllowedException(string message)
            : base(message)
        {
        }

        public FallbackIdentityOperationNotAllowedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FallbackIdentityOperationNotAllowedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Thrown when we were trying to create a client to talk to the legacy SPS identity store (e.g. SPS SU1),
    /// but were not able to do so due to an unexpected response.
    /// </summary>
    [Serializable]
    public class CannotFindLegacySpsIdentityStoreException : IdentityServiceException
    {
        public CannotFindLegacySpsIdentityStoreException()
        {
        }

        public CannotFindLegacySpsIdentityStoreException(string message)
            : base(message)
        {
        }

        public CannotFindLegacySpsIdentityStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CannotFindLegacySpsIdentityStoreException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Unable to restore group scope
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "RestoreGroupScopeValidationException", "Microsoft.VisualStudio.Services.Identity.RestoreGroupScopeValidationException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class RestoreGroupScopeValidationException : IdentityServiceException
    {
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        public RestoreGroupScopeValidationException(string validationError)
            : base(IdentityResources.RestoreGroupScopeValidationError(validationError))
        {
        }

        public RestoreGroupScopeValidationException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RestoreGroupScopeValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
