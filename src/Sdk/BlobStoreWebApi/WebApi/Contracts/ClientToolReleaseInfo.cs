using System;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi.Contracts
{
    /// <summary>
    /// Information about a released client application.
    /// </summary>
    [DataContract]
    public class ClientToolReleaseInfo
    {
        public ClientToolReleaseInfo(string name, string runtimeIdentifier, string version, string filePath, Uri uri)
        {
            Name = name;
            RuntimeIdentifier = runtimeIdentifier;
            Version = version;
            FilePath = filePath;
            Uri = uri;
        }

        /// <summary>
        /// Application's name. Example: ArtifactTool
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// An identifier specifying the platform this release targets. Expect the allowed values to be a subset of 
        /// those officially supported by dotnet core: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog. But 
        /// at minimum these three are always supported: win-x64, linux-x64, osx-x64
        /// </summary>
        [DataMember(Name = "rid")]
        public string RuntimeIdentifier { get; set; }

        /// <summary>
        /// A semantic version of this release, in the format of {Major}.{Minor}.{Build}.{Revision} if all four sections 
        /// exists. {Build} and {Revision} are not required.
        /// </summary>
        [DataMember(Name = "version")]
        public string Version { get; set; }

        /// <summary>
        /// The path to the app in blob storage
        /// </summary>
        [IgnoreDataMember]
        public string FilePath { get; set; }

        /// <summary>
        /// A download URI for this release. The URI is based on an access token that will expire after a certain period.
        /// </summary>
        [DataMember(Name = "uri")]
        public Uri Uri { get; set; }
    }
}
