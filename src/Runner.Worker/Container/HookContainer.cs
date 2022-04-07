using System.Collections.Generic;
using System.Linq;

namespace GitHub.Runner.Worker.Container
{
    public class HookContainer
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Network { get; set; }
        public string NetworkAlias { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }
        public string EntryPointArgs { get; set; }
        public string EntryPoint { get; set; }
        public string WorkingDirectory { get; set; }
        public string CreateOptions { get; private set; }
        public string RuntimePath { get; set; }
        public ContainerRegistry Registry { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public IDictionary<string, string> PortMappings { get; set; }
        public List<MountVolume> MountVolumes { get; set; }
        public HookContainer() { } // For Json deserializer
        public HookContainer(ContainerInfo container)
        {
            Id = container.ContainerId;
            DisplayName = container.ContainerDisplayName;
            Network = container.ContainerNetwork;
            NetworkAlias = container.ContainerNetworkAlias;
            Image = container.ContainerImage;
            Name = container.ContainerName;
            EntryPointArgs = container.ContainerEntryPointArgs;
            EntryPoint = container.ContainerEntryPoint;
            WorkingDirectory = container.ContainerWorkDirectory;
            CreateOptions = container.ContainerCreateOptions;
            RuntimePath = container.ContainerRuntimePath;
            Registry = new ContainerRegistry
            {
                Username = container.RegistryAuthUsername,
                Password = container.RegistryAuthPassword,
                ServerUrl = container.RegistryServer,
            };

            EnvironmentVariables = container.ContainerEnvironmentVariables;
            PortMappings = new Dictionary<string, string>(container.PortMappings.Select(mapping => new KeyValuePair<string, string>(mapping.HostPort, mapping.ContainerPort)));
            MountVolumes = container.MountVolumes;
        }
    }

    public class ContainerRegistry
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ServerUrl { get; set; }
    }
}
