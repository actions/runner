using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an attachment to a build.
    /// </summary>
    [DataContract]
    public class Attachment : BaseSecuredObject
    {
        public Attachment()
        {
        }

        internal Attachment(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The name of the attachment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
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
    }
}
