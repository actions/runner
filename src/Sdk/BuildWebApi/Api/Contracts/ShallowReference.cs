using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// An abstracted reference to some other resource. This class is used to provide the build
    /// data contracts with a uniform way to reference other resources in a way that provides easy
    /// traversal through links.
    /// </summary>
    [Obsolete("Use one of the specific References instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class ShallowReference // TODO: this class is here to maintain binary compat with VS 15 RTW, and should be deleted before dev16 ships
    {
        /// <summary>
        /// Id of the resource
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 Id { get; set; }

        /// <summary>
        /// Name of the linked resource (definition name, controller name, etc.)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Name { get; set; }

        /// <summary>
        /// Full http link to the resource
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Url { get; set; }
    }
}
