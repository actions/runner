using System;

namespace GitHub.Services.Users
{
    /// <summary>
    /// Constants specific to the User Service
    /// </summary>
    // [GenerateAllConstants]
    public static class UserResourceIds
    {
        public const String AreaName = "User";
        public const String AreaId = "970AA69F-E316-4D78-B7B0-B7137E47A22C";
        public static readonly Guid AreaIdGuid = new Guid(AreaId);

        public const String UsersResource = "Users";
        public const String UsersLocation = "61117502-A055-422C-9122-B56E6643ED02";
        public static readonly Guid Users = new Guid(UsersLocation);

        public const String AttributesResource = "Attributes";
        public const String AttributesLocation = "AC77B682-1EF8-4277-AFDE-30AF9B546004";
        public static readonly Guid Attributes = new Guid(AttributesLocation);

        public const String AvatarResource = "Avatar";
        public const String AvatarLocation = "1C34CDF0-DD20-4370-A316-56BA776D75CE";
        public static readonly Guid Avatar = new Guid(AvatarLocation);

        public const String UserDefaultsResource = "UserDefaults";
        public const String UserDefaultsLocation = "A9E65880-7489-4453-AA72-0F7896F0B434";
        public static readonly Guid UserDefaults = new Guid(UserDefaultsLocation);

        public const String MailConfirmationResource = "MailConfirmation";
        public const String MailConfirmationLocation = "FC213DCD-3A4E-4951-A2E2-7E3FED15706D";
        public static readonly Guid MailConfirmation = new Guid(MailConfirmationLocation);

        public const String DescriptorResource = "Descriptor";
        public const String DescriptorLocation = "E338ED36-F702-44D3-8D18-9CBA811D013A";
        public static readonly Guid Descriptor = new Guid(DescriptorLocation);

        public const String StorageKeyResource = "StorageKey";
        public const String StorageKeyLocation = "C1D0BF9E-3220-44D9-B048-222AE15FC3E4";
        public static readonly Guid StorageKey = new Guid(StorageKeyLocation);

        public const String AvatarPreviewResource = "AvatarPreview";
        public const String AvatarPreviewLocation = "AAD154D3-750F-47E6-9898-DC3A2E7A1708";
        public static readonly Guid AvatarPreview = new Guid(AvatarPreviewLocation);

        public const String MostRecentlyAccessedHostsResource = "MostRecentlyAccessedHosts";
        public const String MostRecentlyAccessedHostsLocation = "A72C0174-9DB6-428D-8674-3E57EF050F3D";
        public static readonly Guid MostRecentlyAccessedHosts = new Guid(MostRecentlyAccessedHostsLocation);

        public const String RecentlyAccessedHostsResource = "RecentlyAccessedHosts";
        public const String RecentlyAccessedHostsLocation = "6C416D43-571A-454D-8350-DF3E879CB33D";
        public static readonly Guid RecentlyAccessedHosts = new Guid(RecentlyAccessedHostsLocation);

        // L2
        public const String UsersL2Resource = "UsersL2";
        public const String UsersL2Location = "401ED19A-2DF1-4A67-A5BF-52FE6C9FDEA6";
        public static readonly Guid UsersL2 = new Guid(UsersL2Location);

        public const String UsersByStorageKeyL2Resource = "UsersByStorageKeyL2";
        public const String UsersByStorageKeyL2Location = "AD30C600-CD1F-46DF-8022-CD7152A61C1D";
        public static readonly Guid UsersByStorageKeyL2 = new Guid(UsersByStorageKeyL2Location);

        public const string PrivateAttributesResource = "PrivateAttributes";
        public static readonly Guid PrivateAttributesLocationid = new Guid("BDE78236-6D43-4487-9FA0-1FAFE5357D54");
    }
}
