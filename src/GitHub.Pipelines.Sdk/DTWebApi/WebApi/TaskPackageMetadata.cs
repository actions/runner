using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskPackageMetadata
    {
        public TaskPackageMetadata()
        {
        }

        public TaskPackageMetadata(
            String type,
            String version)
        {
            this.Type = type;
            this.Version = version;
        }

        public TaskPackageMetadata(
            String type,
            String version,
            String url)
        {
            this.Type = type;
            this.Version = version;
            this.Url = url;
        }

        /// <summary>
        /// Gets the name of the package.
        /// </summary>
        [DataMember]
        public String Type
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the version of the package.
        /// </summary>
        [DataMember]
        public String Version
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the url of the package.
        /// </summary>
        [DataMember]
        public String Url
        {
            get;
            internal set;
        }
    }
}
