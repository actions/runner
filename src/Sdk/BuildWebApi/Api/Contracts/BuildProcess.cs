using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a build process.
    /// </summary>
    [DataContract]
    [KnownType(typeof(DesignerProcess))]
    [KnownType(typeof(YamlProcess))]
    [KnownType(typeof(DockerProcess))]
    [KnownType(typeof(JustInTimeProcess))]
    [JsonConverter(typeof(BuildProcessJsonConverter))]
    public class BuildProcess : BaseSecuredObject
    {
        protected BuildProcess(
            Int32 type)
        {
        }

        protected internal BuildProcess(
            Int32 type,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Type = type;
        }

        /// <summary>
        /// The type of the process.
        /// </summary>
        /// <remarks>
        /// See <see cref="ProcessType" /> for a list of valid process types.
        /// </remarks>
        [DataMember(Name = "Type")]
        public Int32 Type
        {
            get;
            internal set;
        }
    }
}
