using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a build option definition.
    /// </summary>
    [DataContract]
    public class BuildOptionDefinitionReference : BaseSecuredObject
    {
        public BuildOptionDefinitionReference()
        {
        }

        internal BuildOptionDefinitionReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the referenced build option.
        /// </summary>
        [DataMember(IsRequired = true, Order = 1)]
        public Guid Id
        {
            get;
            set;
        }
    }
}
