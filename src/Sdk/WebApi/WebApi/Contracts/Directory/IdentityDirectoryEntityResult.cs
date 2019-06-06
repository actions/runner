using System;
using System.Runtime.Serialization;
using GitHub.Services.Identity;
using Newtonsoft.Json;

namespace GitHub.Services.Directories
{
    [DataContract]
    public class IdentityDirectoryEntityResult<TIdentity> : DirectoryEntityResult
        where TIdentity : IVssIdentity
    {
        [DataMember]
        public TIdentity Identity { get; }

        public IdentityDirectoryEntityResult(DirectoryEntityResult result, TIdentity identity)
            : this(identity, result.Entity, result.EntityState, result.Status, result.Exception)
        {
        }

        [JsonConstructor]
        public IdentityDirectoryEntityResult(
            TIdentity identity, 
            IDirectoryEntity entity, 
            string entityState, 
            string status, 
            Exception exception = null)
            : base(entity, entityState, status, exception)
        {
            Identity = identity;
        }
    }
}
