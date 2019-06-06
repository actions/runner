using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Do not attempt to use this type to update user. Use
    /// one of the subclasses instead. This type does not contain 
    /// sufficient fields to create a new user.
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(GraphUserUpdateContextJsonConverter))]
    public abstract class GraphUserUpdateContext
    {
        /// <summary>
        /// Storage key should not be specified in case of updating user
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete()]
        public Guid StorageKey { get; set; }

        //Currently there's a bug on the client generator that if a class doesn't have data member, it wouldn't get generated
        //We're adding a temporary data member here in order to get passed that issue
        //BUG 1466336 has been created to track this issue. Once the bug is fixed, we'll remove this data member.
        //Marking it as obsolete and never use for now to ensure no one can access
    }
    /// <summary>
    /// Use this type to update an existing user using the OriginID as a reference to an existing user from an external
    /// AD or AAD backed provider. This is the subset of GraphUser fields required for creation of
    /// a GraphUser for the AD and AAD use case when looking up the user by its unique ID in the backing provider.
    /// </summary>
    [DataContract]
    public class GraphUserOriginIdUpdateContext : GraphUserUpdateContext
    {
        /// <summary>
        /// This should be the object id or sid of the user from the source AD or AAD provider.
        /// Example: d47d025a-ce2f-4a79-8618-e8862ade30dd
        /// Azure Devops will communicate with the source provider to fill all other fields on creation.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string OriginId { get; set; }
    }
}
