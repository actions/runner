using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public class ArtifactResource : BaseSecuredObject
    {
        public ArtifactResource()
        {
        }

        public ArtifactResource(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The type of the resource: File container, version control folder, UNC path, etc.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// Type-specific data about the artifact.
        /// </summary>
        /// <remarks>
        /// For example, "#/10002/5/drop", "$/drops/5", "\\myshare\myfolder\mydrops\5"
        /// </remarks>
        [DataMember(EmitDefaultValue = false)]
        public String Data
        {
            get;
            set;
        }

        /// <summary>
        /// Type-specific properties of the artifact.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Dictionary<String, String> Properties
        {
            get;
            set;
        }

        /// <summary>
        /// The full http link to the resource.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Url
        {
            get;
            set;
        }

        /// <summary>
        /// A link to download the resource.
        /// </summary>
        /// <remarks>
        /// This might include things like query parameters to download as a zip file.
        /// </remarks>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String DownloadUrl
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
