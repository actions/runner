using System;

namespace GitHub.DistributedTask.WebApi
{
    public static class ServiceEndpointTypes
    {
        /// <summary>
        /// Azure endpoint
        /// </summary>
        public const String Azure = "Azure";

        /// <summary>
        /// Chef endpoint
        /// </summary>
        public const String Chef = "Chef";

        /// Chef endpoint
        /// </summary>
        public const String ExternalTfs = "ExternalTfs";

        /// <summary>
        /// Generic endpoint
        /// </summary>
        public const String Generic = "Generic";

        /// <summary>
        /// GitHub endpoint
        /// </summary>
        public const String GitHub = "GitHub";

        /// <summary>
        /// GitHub Enterprise endpoint
        /// </summary>
        public const String GitHubEnterprise = "GitHubEnterprise";

        /// <summary>
        /// Bitbucket endpoint
        /// </summary>
        public const String Bitbucket = "Bitbucket";

        /// <summary>
        /// SSH endpoint
        /// </summary>
        public const String SSH = "SSH";

        /// <summary>
        /// Subversion endpoint
        /// </summary>
        public const String Subversion = "Subversion";

        /// <summary>
        ///Gcp endpoint
        /// </summary>
        public const String Gcp = "google-cloud";

        /// <summary>
        /// Subversion endpoint
        /// </summary>
        public const String Jenkins = "Jenkins";

        /// <summary>
        /// External Git repository
        /// </summary>
        public const String ExternalGit = "Git";

        /// <summary>
        /// Azure RM endpoint
        /// </summary>
        public const String AzureRM = "AzureRM";

        /// <summary>
        /// Azure Deployment Manager
        /// </summary>
        public const String AzureDeploymentManager = "AzureDeploymentManager";

        /// <summary>
        /// Azure Service Fabric
        /// </summary>
        public const String AzureServiceFabric = "ServiceFabric";

        /// <summary>
        /// Azure Service Fabric
        /// </summary>
        public const String Docker = "dockerregistry";

        /// <summary>
        /// Jira
        /// </summary>
        public const String Jira = "Jira";
    }
}
