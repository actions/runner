using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a work item related to some source item.
    /// These are retrieved from Source Providers.
    /// </summary>
    [DataContract]
    public class SourceRelatedWorkItem : BaseSecuredObject
    {
        public SourceRelatedWorkItem()
        {
        }

        internal SourceRelatedWorkItem(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        ///  The name of the provider the work item is associated with.
        /// </summary>
        [DataMember]
        public String ProviderName { get; set; }

        /// <summary>
        /// Unique identifier for the work item
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id { get; set; }

        /// <summary>
        /// Short name for the work item.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Title { get; set; }

        /// <summary>
        /// Long description for the work item.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description { get; set; }

        /// <summary>
        /// Type of work item, e.g. Bug, Task, User Story, etc.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type { get; set; }

        /// <summary>
        /// Current state of the work item, e.g. Active, Resolved, Closed, etc.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String CurrentState { get; set; }

        /// <summary>
        /// Identity ref for the person that the work item is assigned to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef AssignedTo { get; set; }

        /// <summary>
        /// The links to other objects related to this object.
        /// </summary>
        public ReferenceLinks Links
        {
            get
            {
                if (m_links == null)
                {
                    m_links = new ReferenceLinks();
                }
                return m_links;
            }
        }

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        private ReferenceLinks m_links;
    }
}
