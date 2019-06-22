using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the ability to build forks of the selected repository.
    /// </summary>
    [DataContract]
    public sealed class Forks : BaseSecuredObject
    {
        public Forks()
        {
        }

        internal Forks(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// Indicates whether the trigger should queue builds for forks of the selected repository.
        /// </summary>
        [DataMember]
        public Boolean Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether a build should use secrets when building forks of the selected repository.
        /// </summary>
        [DataMember]
        public Boolean AllowSecrets
        {
            get;
            set;
        }
    }
}
