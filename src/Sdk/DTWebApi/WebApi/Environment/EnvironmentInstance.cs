using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Environment.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class EnvironmentInstance
    {
        /// <summary>
        /// Id of the Environment
        /// </summary>
        [DataMember]
        public Int32 Id
        {
            get;
            set;
        }
        
        /// <summary>
        /// Name of the Environment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }
        
        /// <summary>
        /// Description of the Environment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }
        
        /// <summary>
        /// Identity reference of the user who created the Environment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }
        
        /// <summary>
        /// Creation time of the Environment
        /// </summary>
        [DataMember]
        public DateTime CreatedOn 
        {
            get;
            set; 
        }
        
        /// <summary>
        /// Identity reference of the user who last modified the Environment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef LastModifiedBy
        {
            get;
            set;
        }
        
        /// <summary>
        /// Last modified time of the Environment
        /// </summary>
        [DataMember]
        public DateTime LastModifiedOn 
        {
            get;
            set;
        }

        /// <summary>
        /// List of resources
        /// </summary>
        public IList<EnvironmentResourceReference> Resources
        {
            get
            {
                if (this.resources == null)
                {
                    this.resources = new List<EnvironmentResourceReference>();
                }

                return this.resources;
            }
        }

        /// <summary>
        /// Resources that defined or used for this environment.
        /// We use this for deployment job's resource authorization.
        /// </summary>
        public Pipelines.PipelineResources ReferencedResources
        {
            get;
            set;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Resources")]
        private IList<EnvironmentResourceReference> resources;
    }
}
