using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookContainer : HookArgs
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Network { get; set; }
        public string NetworkAlias { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> EntryPointArgs { get; set; }
        public string EntryPoint { get; set; }
        public string WorkingDirectory { get; set; }
        public string CreateOptions { get; private set; }
        public string RuntimePath { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ContainerRegistry Registry { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> PortMappings { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<MountVolume> MountVolumes { get; set; }
        public HookContainer() { } // For Json deserializer
        public HookContainer(ContainerInfo container)
        {
            Id = container.ContainerId;
            DisplayName = container.ContainerDisplayName;
            Network = container.ContainerNetwork;
            NetworkAlias = container.ContainerNetworkAlias;
            Image = container.ContainerImage;
            Name = container.ContainerName;
            EntryPointArgs = container.ContainerEntryPointArgs.Split(' ').Select(arg => arg.Trim());
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

    public static class ContainerInfoExtensions
    {
        public static HookContainer GetHookContainer(this ContainerInfo containerInfo)
        {
            return new HookContainer(containerInfo);
        }
    }
}
