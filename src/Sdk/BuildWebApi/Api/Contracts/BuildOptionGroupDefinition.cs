using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a group of inputs for a build option.
    /// </summary>
    [DataContract]
    public class BuildOptionGroupDefinition : BaseSecuredObject
    {
        public BuildOptionGroupDefinition()
        {
        }

        internal BuildOptionGroupDefinition(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The internal name of the group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the group to display in the UI.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the group is initially displayed as expanded in the UI.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsExpanded
        {
            get;
            set;
        }       
    }
}
