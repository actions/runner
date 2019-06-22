using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a variable group.
    /// </summary>
    [DataContract]
    public class VariableGroupReference : BaseSecuredObject
    {
        public VariableGroupReference()
            : this(null)
        {
        }

        internal VariableGroupReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// The Name of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Alias
        {
            get;
            set;
        }
    }
}
