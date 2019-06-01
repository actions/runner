using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    /// <summary>
    /// Do not attempt to use this type to create a new user. Use
    /// one of the subclasses instead. This type does not contain 
    /// sufficient fields to create a new user.
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(GraphUserCreationContextJsonConverter))]
    public abstract class GraphUserCreationContext
    {
        /// <summary>
        /// Optional: If provided, we will use this identifier for the storage key of the created user
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid StorageKey { get; set; }
    }

    /// <summary>
    /// Use this type to create a new user using the OriginID as a reference to an existing user from an external
    /// AD or AAD backed provider. This is the subset of GraphUser fields required for creation of
    /// a GraphUser for the AD and AAD use case when looking up the user by its unique ID in the backing provider.
    /// </summary>
    [DataContract]
    public class GraphUserOriginIdCreationContext : GraphUserCreationContext
    {
        /// <summary>
        /// This should be the object id or sid of the user from the source AD or AAD provider.
        /// Example: d47d025a-ce2f-4a79-8618-e8862ade30dd
        /// Team Services will communicate with the source provider to fill all other fields on creation.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string OriginId { get; set; }


        /// <summary>
        /// This should be the name of the origin provider.
        /// Example: github.com
        /// </summary>
        [DataMember(IsRequired = false)]
        public string Origin { get; set; }
    }

    /// <summary>
    /// Use this type to create a new user using the principal name as a reference to an existing user from an external
    /// AD or AAD backed provider. This is the subset of GraphUser fields required for creation of
    /// a GraphUser for the AD and AAD use case when looking up the user by its principal name in the backing provider.
    /// </summary>
    [DataContract]
    public class GraphUserPrincipalNameCreationContext : GraphUserCreationContext
    {
        /// <summary>
        /// This should be the principal name or upn of the user in the source AD or AAD provider.
        /// Example: jamal@contoso.com
        /// Team Services will communicate with the source provider to fill all other fields on creation.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string PrincipalName { get; set; }
    }

    /// <summary>
    /// Use this type to create a new user using the mail address as a reference to an existing user from an external
    /// AD or AAD backed provider. This is the subset of GraphUser fields required for creation of
    /// a GraphUser for the AD and AAD use case when looking up the user by its mail address in the backing provider.
    /// </summary>
    [DataContract]
    public class GraphUserMailAddressCreationContext : GraphUserCreationContext
    {
        /// <summary>
        /// This should be the mail address of the user in the source AD or AAD provider.
        /// Example: Jamal.Hartnett@contoso.com
        /// Team Services will communicate with the source provider to fill all other fields on creation.
        /// </summary
        [DataMember(IsRequired = true)]
        public string MailAddress { get; set; }
    }
}