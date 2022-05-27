using System.Collections.Generic;
using System.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookContainer : HookArgs
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Network { get; set; }
        public string Image { get; set; }
        public IEnumerable<string> EntryPointArgs { get; set; } = new List<string>();
        public string EntryPoint { get; set; }
        public string WorkingDirectory { get; set; }
        public string CreateOptions { get; private set; }
        public ContainerRegistry Registry { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public IEnumerable<string> PortMappings { get; set; } = new List<string>();
        public IEnumerable<MountVolume> SystemMountVolumes { get; set; } = new List<MountVolume>();
        public IEnumerable<MountVolume> UserMountVolumes { get; set; } = new List<MountVolume>();
        public HookContainer() { } // For Json deserializer
        public HookContainer(ContainerInfo container)
        {
            Id = container.ContainerId;
            DisplayName = container.ContainerDisplayName;
            Network = container.ContainerNetwork;
            Image = container.ContainerImage;
            EntryPointArgs = container.ContainerEntryPointArgs?.Split(' ').Select(arg => arg.Trim()) ?? new List<string>();
            EntryPoint = container.ContainerEntryPoint;
            WorkingDirectory = container.ContainerWorkDirectory;
            CreateOptions = container.ContainerCreateOptions;
            Registry = new ContainerRegistry
            {
                Username = container.RegistryAuthUsername,
                Password = container.RegistryAuthPassword,
                ServerUrl = container.RegistryServer,
            };
            EnvironmentVariables = container.ContainerEnvironmentVariables;
            PortMappings = container.UserPortMappings.Select(p => p.Value).ToList();
            SystemMountVolumes = container.SystemMountVolumes;
            UserMountVolumes = container.UserMountVolumes;
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
