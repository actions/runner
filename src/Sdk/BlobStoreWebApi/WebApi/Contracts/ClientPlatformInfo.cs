using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi.Contracts
{
    /// <summary>
    /// The information about client platform to run the application.
    /// </summary>
    [DataContract]
    public class ClientPlatformInfo
    {
        /// <summary>
        /// OS info, including OS family, distro and version.
        /// </summary>
        [DataMember(Name = "os")]
        public OSInfo OSInfo { get; set; }

        /// <summary>
        /// The machine type / CPU architecture. Example: AMD64, x86_64, arm
        /// </summary>
        [DataMember(Name = "arch")]
        public string Architecture { get; set; }
    }

    /// <summary>
    /// Information about operating system, including OS family, distro and version.
    /// </summary>
    [DataContract]
    public class OSInfo
    {
        /// <summary>
        /// Operating system name, e.g. Windows, Linux, Darwin (macOS)
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Rough information about kernel version, e.g. "10" or "4.15.0-20-generic"
        /// </summary>
        [DataMember(Name = "release")]
        public string Release { get; set; }

        /// <summary>
        /// Detailed information about kernel version, e.g. "10.0.14393" or "#21-Ubuntu SMP Tue Apr 24 06:16:15 UTC 2018"
        /// </summary>
        [DataMember(Name = "version")]
        public string Version { get; set; }

        /// <summary>
        /// Distribution name. Applicable only to certain OS platforms such as Linux. e.g. Ubuntu, Fedora, CentOS
        /// </summary>
        [DataMember(Name = "distroName")]
        public string DistributionName { get; set; }

        /// <summary>
        /// Distribution version. Applicable only to certain OS platforms such as Linux. e.g. 18.04
        /// </summary>
        [DataMember(Name = "distroVersion")]
        public string DistributionVersion { get; set; }
    }
}
