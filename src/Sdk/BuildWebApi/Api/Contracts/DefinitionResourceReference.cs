using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public class DefinitionResourceReference : BaseSecuredObject
    {
        public DefinitionResourceReference()
            : this(null)
        {
        }

        internal DefinitionResourceReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// A friendly name for the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// The id of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the resource is authorized for use.
        /// </summary>
        [DataMember]
        public Boolean Authorized
        {
            get;
            set;
        }
    }
}
