using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ContainerPropertyNames
    {
        public const String Env = "env";
        public const String Image = "image";
        public const String Options = "options";
        public const String Volumes = "volumes";
        public const String Ports = "ports";
    }

    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ContainerResource : Resource
    {
        [JsonConstructor]
        public ContainerResource()
        {
        }

        private ContainerResource(ContainerResource referenceToCopy)
            : base(referenceToCopy)
        {
        }

        /// <summary>
        /// Gets or sets the environment which is provided to the container.
        /// </summary>
        public IDictionary<String, String> Environment
        {
            get
            {
                return this.Properties.Get<IDictionary<String, String>>(ContainerPropertyNames.Env);
            }
            set
            {
                this.Properties.Set(ContainerPropertyNames.Env, value);
            }
        }

        /// <summary>
        /// Gets or sets the container image name.
        /// </summary>
        public String Image
        {
            get
            {
                return this.Properties.Get<String>(ContainerPropertyNames.Image);
            }
            set
            {
                this.Properties.Set(ContainerPropertyNames.Image, value);
            }
        }

        /// <summary>
        /// Gets or sets the options used for the container instance.
        /// </summary>
        public String Options
        {
            get
            {
                return this.Properties.Get<String>(ContainerPropertyNames.Options);
            }
            set
            {
                this.Properties.Set(ContainerPropertyNames.Options, value);
            }
        }

        /// <summary>
        /// Gets or sets the volumes which are mounted into the container.
        /// </summary>
        public IList<String> Volumes
        {
            get
            {
                return this.Properties.Get<IList<String>>(ContainerPropertyNames.Volumes);
            }
            set
            {
                this.Properties.Set(ContainerPropertyNames.Volumes, value);
            }
        }

        /// <summary>
        /// Gets or sets the ports which are exposed on the container.
        /// </summary>
        public IList<String> Ports
        {
            get
            {
                return this.Properties.Get<IList<String>>(ContainerPropertyNames.Ports);
            }
            set
            {
                this.Properties.Set(ContainerPropertyNames.Ports, value);
            }
        }

        public ContainerResource Clone()
        {
            return new ContainerResource(this);
        }
    }
}
