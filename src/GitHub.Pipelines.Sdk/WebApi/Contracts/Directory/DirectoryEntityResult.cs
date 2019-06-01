using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Directories
{
    [DataContract]
    public class DirectoryEntityResult
    {
        [DataMember]
        [JsonConverter(typeof(DirectoryEntityJsonConverter))]
        public IDirectoryEntity Entity { get; }

        /// <summary>
        /// The <see cref="DirectoryEntityState"/> of this result. 
        /// </summary>
        [DataMember]
        public string EntityState { get; }

        /// <summary>
        /// The <see cref="DirectoryStatus"/> of this result.
        /// </summary>
        [DataMember]
        public string Status { get; }

        /// <summary>
        /// The exception associated with this result.
        /// This property will be null when <see cref="Status"/> is <see cref="DirectoryStatus.Success"/>,
        /// and may or may not be set otherwise.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Exception Exception { get; }

        public DirectoryEntityResult(IDirectoryEntity entity, string entityState, string status, Exception exception = null)
        {
            Entity = entity;
            EntityState = entityState;
            Status = status;
            Exception = exception;
        }
    }
}
