using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a dependency.
    /// </summary>
    [DataContract]
    public class Dependency : BaseSecuredObject
    {
        public Dependency()
        {
        }

        internal Dependency(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The scope. This names the object referenced by the dependency.
        /// </summary>
        [DataMember]
        public String Scope
        {
            get;
            set;
        }

        /// <summary>
        /// The event. The dependency is satisfied when the referenced object emits this event.
        /// </summary>
        [DataMember]
        public String Event
        {
            get;
            set;
        }
    }
}
