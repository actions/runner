using System;
using System.IO;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Container
{
    [ServiceLocator(Default = typeof(ContainerRegistryManager))]
    public interface IContainerRegistryManager : IRunnerService
    {
        Task<string> ContainerRegistryLogin(IExecutionContext executionContext, ContainerInfo container);
        void ContainerRegistryLogout(string configLocation);
        void UpdateRegistryAuthForGitHubToken(IExecutionContext executionContext, ContainerInfo container);
    }

    public class ContainerRegistryManager : RunnerService, IContainerRegistryManager
    {
        private IDockerCommandManager dockerManager;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            dockerManager = HostContext.GetService<IDockerCommandManager>();
        }

        public void UpdateRegistryAuthForGitHubToken(IExecutionContext executionContext, ContainerInfo container)
        {
            var registryIsTokenCompatible = container.RegistryServer.Equals("ghcr.io", StringComparison.OrdinalIgnoreCase) || container.RegistryServer.Equals("containers.pkg.github.com", StringComparison.OrdinalIgnoreCase);
            var isFallbackTokenFromHostedGithub = HostContext.GetService<IConfigurationStore>().GetSettings().IsHostedServer;
            if (!registryIsTokenCompatible || !isFallbackTokenFromHostedGithub)
            {
                return;
            }

            var registryCredentialsNotSupplied = string.IsNullOrEmpty(container.RegistryAuthUsername) && string.IsNullOrEmpty(container.RegistryAuthPassword);
            if (registryCredentialsNotSupplied)
            {
                container.RegistryAuthUsername = executionContext.GetGitHubContext("actor");
                container.RegistryAuthPassword = executionContext.GetGitHubContext("token");
            }
        }
        public async Task<string> ContainerRegistryLogin(IExecutionContext executionContext, ContainerInfo container)
        {
            if (string.IsNullOrEmpty(container.RegistryAuthUsername) || string.IsNullOrEmpty(container.RegistryAuthPassword))
            {
                // No valid client config can be generated
                return "";
            }
            var configLocation = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), $".docker_{Guid.NewGuid()}");
            try
            {
                var dirInfo = Directory.CreateDirectory(configLocation);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to create directory to store registry client credentials: {e.Message}");
            }
            var loginExitCode = await dockerManager.DockerLogin(
                executionContext,
                configLocation,
                container.RegistryServer,
                container.RegistryAuthUsername,
                container.RegistryAuthPassword);

            if (loginExitCode != 0)
            {
                throw new InvalidOperationException($"Docker login for '{container.RegistryServer}' failed with exit code {loginExitCode}");
            }
            return configLocation;
        }
        public void ContainerRegistryLogout(string configLocation)
        {
            try
            {
                if (!string.IsNullOrEmpty(configLocation) && Directory.Exists(configLocation))
                {
                    Directory.Delete(configLocation, recursive: true);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to remove directory containing Docker client credentials: {e.Message}");
            }
        }

    }
}
