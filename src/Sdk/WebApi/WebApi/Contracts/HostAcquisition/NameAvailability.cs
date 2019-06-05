using System.Runtime.Serialization;

namespace GitHub.Services.HostAcquisition
{
    [DataContract]
    public sealed class NameAvailability
    {
        /// <summary>
        /// Name requested.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// True if the name is available; False otherwise.
        /// See the unavailability reason for an explanation.
        /// </summary>
        [DataMember]
        public bool IsAvailable { get; set; }

        /// <summary>
        /// The reason why IsAvailable is False or Null
        /// </summary>
        [DataMember(IsRequired = false)]
        public string UnavailabilityReason { get; set; }
    }
}
