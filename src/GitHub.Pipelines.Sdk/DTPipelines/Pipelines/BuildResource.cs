using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BuildPropertyNames
    {
        public static readonly String Branch = "branch";
        public static readonly String Connection = "connection";
        public static readonly String Source = "source";
        public static readonly String Type = "type";
        public static readonly String Version = "version";
    }

    /// <summary>
    /// Provides a data contract for a build resource referenced by a pipeline.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BuildResource : Resource
    {
        public BuildResource()
        {
        }

        protected BuildResource(BuildResource resourceToCopy)
            : base(resourceToCopy)
        {
        }

        /// <summary>
        /// Gets or sets the type of build resource.
        /// </summary>
        public String Type
        {
            get
            {
                return this.Properties.Get<String>(BuildPropertyNames.Type);
            }
            set
            {
                this.Properties.Set(BuildPropertyNames.Type, value);
            }
        }

        /// <summary>
        /// Gets or sets the version of the build resource.
        /// </summary>
        public String Version
        {
            get
            {
                return this.Properties.Get<String>(BuildPropertyNames.Version);
            }
            set
            {
                this.Properties.Set(BuildPropertyNames.Version, value);
            }
        }

        public BuildResource Clone()
        {
            return new BuildResource(this);
        }
    }
}
