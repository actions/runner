using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PipelinePropertyNames
    {
        public static readonly String Artifacts = "artifacts";
        public static readonly String Branch = "branch";
        public static readonly String DefinitionId = "definitionId";
        public static readonly String PipelineId = "pipelineId";
        public static readonly String Project = "project";
        public static readonly String ProjectId = "projectId";
        public static readonly String Source = "source";
        public static readonly String Tags = "tags";
        public static readonly String Version = "version";
    }

    /// <summary>
    /// Provides a data contract for a pipeline resource referenced by a pipeline.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineResource : Resource
    {
        public PipelineResource()
        {
        }

        protected PipelineResource(PipelineResource resourceToCopy)
            : base(resourceToCopy)
        {
        }

        /// <summary>
        /// Gets or sets the version of the build resource.
        /// </summary>
        public String Version
        {
            get
            {
                return this.Properties.Get<String>(PipelinePropertyNames.Version);
            }
            set
            {
                this.Properties.Set(PipelinePropertyNames.Version, value);
            }
        }

        public PipelineResource Clone()
        {
            return new PipelineResource(this);
        }
    }
}