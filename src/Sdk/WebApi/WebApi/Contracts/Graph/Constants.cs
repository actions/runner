using System;
using System.Collections.Generic;
using GitHub.Services.Common;

namespace GitHub.Services.Graph
{
    public static class Constants
    {
        static Constants()
        {
            // For the normalization of incoming IdentityType strings.
            // This is an optimization; it is not required that any particular IdentityType values
            // appear in this list, but it helps performance to have common values here
            var subjectTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { SubjectType.AadUser, SubjectType.AadUser },
                { SubjectType.MsaUser, SubjectType.MsaUser },
                { SubjectType.UnknownUser, SubjectType.UnknownUser },
                { SubjectType.AadGroup, SubjectType.AadGroup },
                { SubjectType.VstsGroup, SubjectType.VstsGroup },
                { SubjectType.UnknownGroup, SubjectType.UnknownGroup },
                { SubjectType.BindPendingUser, SubjectType.BindPendingUser },
                { SubjectType.WindowsIdentity, SubjectType.WindowsIdentity },
                { SubjectType.UnauthenticatedIdentity, SubjectType.UnauthenticatedIdentity },
                { SubjectType.ServiceIdentity, SubjectType.ServiceIdentity },
                { SubjectType.AggregateIdentity, SubjectType.AggregateIdentity },
                { SubjectType.ImportedIdentity, SubjectType.ImportedIdentity },
                { SubjectType.ServerTestIdentity, SubjectType.ServerTestIdentity },
                { SubjectType.GroupScopeType, SubjectType.GroupScopeType },
                { SubjectType.CspPartnerIdentity, SubjectType.CspPartnerIdentity },
                { SubjectType.SystemServicePrincipal, SubjectType.SystemServicePrincipal },
                { SubjectType.SystemLicense, SubjectType.SystemLicense },
                { SubjectType.SystemPublicAccess, SubjectType.SystemPublicAccess},
                { SubjectType.SystemAccessControl, SubjectType.SystemAccessControl },
                { SubjectType.SystemScope, SubjectType.SystemScope },
                { SubjectType.AcsServiceIdentity, SubjectType.AcsServiceIdentity },
                { SubjectType.Unknown, SubjectType.Unknown },
            };

            SubjectTypeMap = subjectTypeMap;

            var socialTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { SocialType.GitHub, SocialType.GitHub },
                { SocialType.Unknown, SocialType.Unknown },
            };

            SocialTypeMap = socialTypeMap;
        }

        [GenerateSpecificConstants]
        public static class SubjectKind
        {
            [GenerateConstant]
            public const string Group = "group";
            public const string Scope = "scope";
            [GenerateConstant]
            public const string User = "user";
            public const string SystemSubject = "systemSubject";
        }

        [GenerateSpecificConstants]
        public static class SubjectType
        {
            [GenerateConstant]
            public const string AadUser = "aad";
            [GenerateConstant]
            public const string MsaUser = "msa";
            public const string UnknownUser = "unusr"; // user with unknown type (not add nor msa)
            [GenerateConstant]
            public const string AadGroup = "aadgp";
            [GenerateConstant]
            public const string VstsGroup = "vssgp";
            public const string UnknownGroup = "ungrp"; // group with unknown type (not add nor vsts)
            [GenerateConstant]
            public const string BindPendingUser = "bnd";
            public const string WindowsIdentity = "win";
            public const string UnauthenticatedIdentity = "uauth";
            public const string ServiceIdentity = "svc";
            public const string AggregateIdentity = "agg";
            public const string ImportedIdentity = "imp";
            public const string ServerTestIdentity = "tst";
            public const string GroupScopeType = "scp";
            public const string CspPartnerIdentity = "csp";
            public const string SystemServicePrincipal = "s2s";
            public const string SystemLicense = "slic";
            public const string SystemScope = "sscp";
            public const string SystemCspPartner = "scsp";
            public const string SystemPublicAccess = "spa";
            public const string SystemAccessControl = "sace";
            public const string AcsServiceIdentity = "acs";
            public const string Unknown = "ukn"; // none of the above
        }

        public static readonly IReadOnlyDictionary<String, String> SubjectTypeMap;

        [GenerateSpecificConstants]
        public static class SocialType
        {
            public const string GitHub = "ghb";
            public const string Unknown = "ukn";
        }

        public static readonly IReadOnlyDictionary<String, String> SocialTypeMap;

        [GenerateSpecificConstants]
        public static class UserMetaType
        {
            public const string Member = "member";
            [GenerateConstant]
            public const string Guest = "guest";
            public const string CompanyAdministrator = "companyAdministrator";
            public const string HelpdeskAdministrator = "helpdeskAdministrator";
        }

        internal static class SubjectDescriptorPolicies
        {
            internal const int MaxSubjectTypeLength = 5;
            internal const int MinSubjectTypeLength = 3;
            internal const int MinSubjectDescriptorStringLength = 6;
            internal const int MaxIdentifierLength = 256;
        }

        internal static class SocialDescriptorPolicies
        {
            internal const int MaxSocialTypeLength = 4;
            internal const int MinSocialTypeLength = SubjectDescriptorPolicies.MinSubjectTypeLength;
            internal const int MinSocialDescriptorStringLength = SubjectDescriptorPolicies.MinSubjectDescriptorStringLength;
            internal const int MaxIdentifierLength = SubjectDescriptorPolicies.MaxIdentifierLength;
        }

        public const char SubjectDescriptorPartsSeparator = '.';

        // Social descriptor constants
        public const char SocialListSeparator = ',';
        public const char SocialDescriptorPartsSeparator = '.';
        public const string SocialDescriptorPrefix = "@";
    }
}
