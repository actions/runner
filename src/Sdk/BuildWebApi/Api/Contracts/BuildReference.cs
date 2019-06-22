using GitHub.Services.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a build.
    /// </summary>
    [DataContract]
    public class BuildReference : BaseSecuredObject
    {
        public BuildReference()
        {
        }

        internal BuildReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [Key]
        public Int32 Id
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The build number.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String BuildNumber
        {
            get;
            set;
        }

        /// <summary>
        /// The build status.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// The build result.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildResult? Result
        {
            get;
            set;
        }

        /// <summary>
        /// The time that the build was queued.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? QueueTime
        {
            get;
            set;
        }

        /// <summary>
        /// The time that the build was started.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            set;
        }

        /// <summary>
        /// The time that the build was completed.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        /// <summary>
        /// The identity on whose behalf the build was queued.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef RequestedFor
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }
        
        /// <summary>
        /// Indicates whether the build has been deleted.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Deleted
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
