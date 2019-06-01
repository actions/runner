using System;
using System.Runtime.Serialization;
using GroupScopeType = Microsoft.VisualStudio.Services.Identity.GroupScopeType;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    /// <summary>
    /// This type is the subset of fields that can be provided by the user to create
    /// a Vsts scope. Scope creation is currently limited to internal back-compat scenarios.
    /// End users that attempt to create a scope with this API will fail.
    /// </summary>
    [DataContract]
    public class GraphScopeCreationContext
    {
        /// <summary>
        /// The scope must be provided with a unique name within the parent scope. This means 
        /// the created scope can have a parent or child with the same name, but no siblings 
        /// with the same name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// The type of scope being created. 
        /// </summary>
        [DataMember]
        public GroupScopeType ScopeType { get; set; }

        /// <summary>
        /// An optional ID that uniquely represents the scope within it's parent scope. If
        /// this parameter is not provided, Vsts will generate on automatically.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid StorageKey { get; set; }

        /// <summary>
        /// Set this optional field if this scope is created on behalf of a user other than the
        /// user making the request. This should be the Id of the user that is not the requester.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid CreatorId { get; set; }

        /// <summary>
        /// All scopes have an Administrator Group that controls access to the contents of the 
        /// scope. Set this field to use a non-default group name for that administrators group.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string AdminGroupName { get; set; }

        /// <summary>
        /// Set this field to override the default description of this scope's admin group.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string AdminGroupDescription { get; set; }
    }
}
