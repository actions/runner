using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents a downloadable package.
    /// </summary>
    [DataContract]
    public class PackageMetadata
    {
        /// <summary>
        /// The type of package (e.g. "agent")
        /// </summary>
        [DataMember]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// The platform (win7, linux, etc.)
        /// </summary>
        [DataMember]
        public String Platform
        {
            get;
            set;
        }

        /// <summary>
        /// The date the package was created
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime CreatedOn
        {
            get;
            set;
        }

        /// <summary>
        /// The package version.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public PackageVersion Version
        {
            get;
            set;
        }

        /// <summary>
        /// A direct link to download the package.
        /// </summary>
        [DataMember]
        public String DownloadUrl
        {
            get;
            set;
        }

        /// <summary>
        /// File ID in file service
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32? FileId
        {
            get;
            set;
        }

        /// <summary>
        /// Auth token to download the package
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Token
        {
            get;
            set;
        }

        /// <summary>
        /// SHA256 hash
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String HashValue
        {
            get;
            set;
        }

        /// <summary>
        /// A link to documentation
        /// </summary>
        [DataMember]
        public String InfoUrl
        {
            get;
            set;
        }

        /// <summary>
        /// The UI uses this to display instructions, e.g. "unzip MyAgent.zip"
        /// </summary>
        [DataMember]
        public String Filename
        {
            get;
            set;
        }
    }
}
