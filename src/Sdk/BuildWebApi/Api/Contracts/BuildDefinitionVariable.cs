using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a variable used by a build definition.
    /// </summary>
    [DataContract]
    public class BuildDefinitionVariable : BaseSecuredObject
    {
        public BuildDefinitionVariable()
        {
        }

        internal BuildDefinitionVariable(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        private BuildDefinitionVariable(BuildDefinitionVariable variableToClone)
        {
            Value = variableToClone.Value;
            AllowOverride = variableToClone.AllowOverride;
            IsSecret = variableToClone.IsSecret;
        }

        /// <summary>
        /// The value of the variable.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public String Value
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the value can be set at queue time.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean AllowOverride
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the variable's value is a secret.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsSecret
        {
            get;
            set;
        }

        /// <summary>
        /// A clone of this BuildDefinitionVariable.
        /// </summary>
        /// <returns>A new BuildDefinitionVariable</returns>
        public BuildDefinitionVariable Clone()
        {
            return new BuildDefinitionVariable(this);
        }
    }
}
