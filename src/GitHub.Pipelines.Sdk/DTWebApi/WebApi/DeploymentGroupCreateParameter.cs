using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Properties to create Deployment group.
    /// </summary>
    [DataContract]
    public class DeploymentGroupCreateParameter
    {
        /// <summary>
        /// Name of the deployment group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Description of the deployment group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// Identifier of the deployment pool in which deployment agents are registered.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 PoolId
        {
            get;
            set;
        }

        /// <summary>
        /// Deployment pool in which deployment agents are registered.
        /// This is obsolete. Kept for compatibility. Will be marked obsolete explicitly by M132.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [ClientInternalUseOnly(OmitFromTypeScriptDeclareFile = false)]
        public DeploymentGroupCreateParameterPoolProperty Pool
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Properties of Deployment pool to create Deployment group.
    /// </summary>
    [DataContract]
    public class DeploymentGroupCreateParameterPoolProperty
    {
        /// <summary>
        /// Deployment pool identifier.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }
    }
}
