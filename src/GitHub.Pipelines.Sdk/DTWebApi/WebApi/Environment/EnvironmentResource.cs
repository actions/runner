using System;
using System.ComponentModel;
using System.Runtime.Serialization;

using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public abstract class EnvironmentResource
    {
        [DataMember]
        public Int32 Id { get; set; }

        [DataMember]
        public String Name { get; set; }

        /// <summary>
        /// Environment resource type
        /// </summary>
        [DataMember]
        public EnvironmentResourceType Type { get; set; }

        [DataMember]
        public IdentityRef CreatedBy { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public IdentityRef LastModifiedBy { get; set; }

        [DataMember]
        public DateTime LastModifiedOn { get; set; }

        [DataMember]
        public EnvironmentReference EnvironmentReference { get; set; }

        protected EnvironmentResource()
        {
            Name = string.Empty;

            CreatedBy = new IdentityRef();

            LastModifiedBy = new IdentityRef();

            this.EnvironmentReference = new EnvironmentReference();
        }
    }
}
