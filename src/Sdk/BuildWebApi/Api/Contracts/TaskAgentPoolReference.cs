using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to an agent pool.
    /// </summary>
    [DataContract]
    public class TaskAgentPoolReference : BaseSecuredObject
    {
        public TaskAgentPoolReference()
        {
        }

        public TaskAgentPoolReference(Int32 id)
            : this(id, null)
        {
        }

        internal TaskAgentPoolReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        internal TaskAgentPoolReference(
            Int32 id,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Id = id;
        }

        /// <summary>
        /// The pool ID.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// The pool name.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// A value indicating whether or not this pool is managed by the service.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsHosted
        {
            get;
            set;
        }
    }
}
