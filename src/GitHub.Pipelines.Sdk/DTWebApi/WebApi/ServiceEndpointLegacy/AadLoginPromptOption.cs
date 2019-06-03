using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum AadLoginPromptOption
    {
        /// <summary>
        /// Do not provide a prompt option
        /// </summary>
        [EnumMember]
        NoOption = 0,

        /// <summary>
        /// Force the user to login again.  
        /// </summary>
        [EnumMember]
        Login = 1,

        /// <summary>
        /// Force the user to select which account they are logging in with instead of
        /// automatically picking the user up from the session state.
        /// NOTE: This does not work for switching bewtween the variants of a dual-homed user.
        /// </summary>
        [EnumMember]
        SelectAccount = 2,
        
        /// <summary>
        /// Force the user to login again.  
        /// <remarks>
        /// Ignore current authentication state and force the user to authenticate again. This option should be used instead of Login.
        /// </remarks>
        /// </summary>
        [EnumMember]
        FreshLogin = 3,

        /// <summary>
        /// Force the user to login again with mfa.  
        /// <remarks>
        /// Ignore current authentication state and force the user to authenticate again. This option should be used instead of Login, if MFA is required.
        /// </remarks>
        /// </summary>
        [EnumMember]
        FreshLoginWithMfa = 4
    }
}
