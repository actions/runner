using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an item in a repository from a source provider.
    /// </summary>
    [DataContract]
    public class SourceRepositoryItem
    {
        /// <summary>
        ///  The type of the item (folder, file, etc).
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type { get; set; }

        /// <summary>
        ///  Whether the item is able to have sub-items (e.g., is a folder).
        /// </summary>
        [DataMember]
        public Boolean IsContainer { get; set; }

        /// <summary>
        /// The full path of the item, relative to the root of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Path { get; set; }

        /// <summary>
        /// The URL of the item.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Url { get; set; }
    }
}
