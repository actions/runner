using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a build badge.
    /// </summary>
    [DataContract]
    public class BuildBadge
    {
        public BuildBadge()
        {
        }

        /// <summary>
        /// The ID of the build represented by this badge.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 BuildId
        {
            get;
            set;
        }

        /// <summary>
        /// A link to the SVG resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ImageUrl
        {
            get;
            set;
        }
    }
}
