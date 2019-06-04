using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Settings
{
    /// <summary>
    /// Whether a setting applies to a single user or to all users
    /// </summary>
    [DataContract]
    public struct SettingsUserScope
    {
        private const String c_globalMeScopeName = "globalme";
        private const String c_meScopeName = "me";
        private const String c_hostScopeName = "host";

        /// <summary>
        /// Settings for current user which span organizations
        /// </summary>
        public static SettingsUserScope GlobalUser = new SettingsUserScope(isGlobalScoped: true, isUserScoped: true, userId: Guid.Empty);

        /// <summary>
        /// Scope for a setting which applies to the current request's user
        /// </summary>
        public static SettingsUserScope User = new SettingsUserScope(isGlobalScoped: false, isUserScoped: true, userId: Guid.Empty);

        /// <summary>
        /// Scope for a setting which applies to all users
        /// </summary>
        public static SettingsUserScope AllUsers = new SettingsUserScope(isGlobalScoped: false, isUserScoped: false, userId: Guid.Empty);

        /// <summary>
        /// Gets a scope for settings that apply to a specific user
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <returns></returns>
        public static SettingsUserScope SpecificUser(Guid userId)
        {
            return new SettingsUserScope(false, true, userId);
        }

        /// <summary>
        /// Parse a SettingsUserScope from its string identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static SettingsUserScope Parse(String identifier)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(identifier, "identifier");

            if (String.Equals(c_meScopeName, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return SettingsUserScope.User;
            }
            else if (String.Equals(c_hostScopeName, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return SettingsUserScope.AllUsers;
            }
            else if (String.Equals(c_globalMeScopeName, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return SettingsUserScope.GlobalUser;
            }
            else
            {
                Guid userId;
                if (Guid.TryParse(identifier, out userId))
                {
                    return SettingsUserScope.SpecificUser(userId);
                }
                else
                {
                    throw new ArgumentException("userId");
                }
            }
        }

        private SettingsUserScope(Boolean isGlobalScoped, Boolean isUserScoped, Guid userId)
        {
            IsUserScoped = isUserScoped;
            IsGlobalScoped = isGlobalScoped;
            UserId = userId;

            if (isGlobalScoped && !isUserScoped)
            {
                throw new ArgumentException("Only user scope can be global");
            }
        }

        [DataMember(EmitDefaultValue = true)]
        public Guid UserId { get; private set; }

        [DataMember]
        public Boolean IsUserScoped { get; private set; }

        [DataMember]
        public Boolean IsGlobalScoped { get; private set; }

        public override string ToString()
        {
            if (IsUserScoped)
            {
                if (UserId == Guid.Empty)
                {
                    return c_meScopeName;
                }
                else
                {
                    return UserId.ToString();
                }
            }
            else
            {
                return c_hostScopeName;
            }
        }
    }
}
