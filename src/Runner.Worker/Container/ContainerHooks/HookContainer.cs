using System.Collections.Generic;
using System.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookContainer : IHookArgs
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
            Id = container.ContainerId ?? string.Empty;
            DisplayName = container.ContainerDisplayName ?? string.Empty;
            Network = container.ContainerNetwork ?? string.Empty;
            NetworkAlias = container.ContainerNetworkAlias ?? string.Empty;
            Image = container.ContainerImage ?? string.Empty;
            Name = container.ContainerName ?? string.Empty;
            EntryPointArgs = container.ContainerEntryPointArgs ?? string.Empty;
            EntryPoint = container.ContainerEntryPoint ?? string.Empty;
            WorkingDirectory = container.ContainerWorkDirectory ?? string.Empty;
            CreateOptions = container.ContainerCreateOptions ?? string.Empty;
            RuntimePath = container.ContainerRuntimePath ?? string.Empty;
            Registry = new ContainerRegistry
            {
                Username = container.RegistryAuthUsername ?? string.Empty,
                Password = container.RegistryAuthPassword ?? string.Empty,
                ServerUrl = container.RegistryServer ?? string.Empty,
            };

            EnvironmentVariables = container.ContainerEnvironmentVariables ?? new Dictionary<string, string>();
            PortMappings = new Dictionary<string, string>(container.PortMappings.Select(mapping => new KeyValuePair<string, string>(mapping.HostPort, mapping.ContainerPort)));
            MountVolumes = container.MountVolumes ?? new List<MountVolume>();
        }
    }

    public static class ContainerInfoExtensions
    {
        public static HookContainer GetHookContainer(this ContainerInfo containerInfo)
        {
            return new HookContainer(containerInfo);
        }
    }
}
