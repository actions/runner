using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a queue for running builds.
    /// </summary>
    [DataContract]
#pragma warning disable 618
    public class AgentPoolQueue : ShallowReference, ISecuredObject
#pragma warning restore 618
    {
        public AgentPoolQueue()
        {
        }

        internal AgentPoolQueue(
            ISecuredObject securedObject)
        {
            this.m_securedObject = securedObject;
        }

        /// <summary>
        /// The ID of the queue.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public new Int32 Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        /// <summary>
        /// The name of the queue.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public new String Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// The full http link to the resource.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public new String Url
        {
            get
            {
                return base.Url;
            }
            set
            {
                base.Url = value;
            }
        }

        /// <summary>
        /// The pool used by this queue.
        /// </summary>
        [DataMember]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }

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

        #region ISecuredObject implementation

        [IgnoreDataMember]
        Guid ISecuredObject.NamespaceId
        {
            get
            {
                ArgumentUtility.CheckForNull(m_securedObject, nameof(m_securedObject));
                return m_securedObject.NamespaceId;
            }
        }

        [IgnoreDataMember]
        Int32 ISecuredObject.RequiredPermissions
        {
            get
            {
                ArgumentUtility.CheckForNull(m_securedObject, nameof(m_securedObject));
                return m_securedObject.RequiredPermissions;
            }
        }

        String ISecuredObject.GetToken()
        {
            ArgumentUtility.CheckForNull(m_securedObject, nameof(m_securedObject));
            return m_securedObject.GetToken();
        }

        private ISecuredObject m_securedObject;

        #endregion
    }
}
