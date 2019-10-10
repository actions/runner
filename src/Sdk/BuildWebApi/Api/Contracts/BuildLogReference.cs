using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a build log.
    /// </summary>
    [DataContract]
    public class BuildLogReference : BaseSecuredObject
    {
        public BuildLogReference()
        {
        }

        internal BuildLogReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the log.
        /// </summary>
        // EmitDefaultValue is true to ensure that id = 0 is sent for XAML builds' "ActivityLog.xml"
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the log location.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// A full link to the log resource.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Url
        {
            get;
            set;
        }
    }
}
