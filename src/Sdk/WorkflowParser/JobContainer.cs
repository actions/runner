#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;

namespace GitHub.Actions.WorkflowParser
{
    public sealed class JobContainer
    {

        /// <summary>
        /// Gets or sets the environment which is provided to the container.
        /// </summary>
        public IDictionary<String, String> Environment
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the container image name.
        /// </summary>
        public String Image
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the options used for the container instance.
        /// </summary>
        public String Options
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the volumes which are mounted into the container.
        /// </summary>
        public IList<String> Volumes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ports which are exposed on the container.
        /// </summary>
        public IList<String> Ports
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the credentials used for pulling the container iamge.
        /// </summary>
        public ContainerRegistryCredentials Credentials
        {
            get;
            set;
        }
    }

    public sealed class ContainerRegistryCredentials
    {
        /// <summary>
        /// Gets or sets the user to authenticate to a registry with
        /// </summary>
        public String Username
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the password to authenticate to a registry with
        /// </summary>
        public String Password
        {
            get;
            set;
        }
    }
}
