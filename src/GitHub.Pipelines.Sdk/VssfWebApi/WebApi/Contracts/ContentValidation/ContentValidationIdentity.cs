using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Representation of ReporteeName we send to CVS and use for callback actions.
    /// </summary>
    [DataContract, ClientIgnore]
    public class ContentValidationIdentity
    {
        [JsonConstructor]
        public ContentValidationIdentity()
        {
        }

        public ContentValidationIdentity(Identity.Identity source)
        {
            DisplayName = source.ProviderDisplayName;
            SubjectDescriptor = source.SubjectDescriptor;
        }

        /// <summary>
        /// Display name of the user. Could be missing if it would cause us to exceed 128 characters in
        /// serialized length. <see cref="SubjectDescriptor"/> will always be set.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <remarks>Hint: this can be converted back into a VSID by:
        /// https://api.vstsusers.visualstudio.com/{subjectDescriptor}/storagekey
        /// </remarks>
        [DataMember(Order = 2)]
        public SubjectDescriptor SubjectDescriptor { get; set; }
    }
}
