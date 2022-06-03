using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookInput
    {
        public HookCommand Command { get; set; }
        public string ResponseFile { get; set; }
        public HookArgs Args { get; set; }
        public JToken State { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum HookCommand
    {
        [EnumMember(Value = "prepare_job")]
        PrepareJob,
        [EnumMember(Value = "cleanup_job")]
        CleanupJob,
        [EnumMember(Value = "run_script_step")]
        RunScriptStep,
        [EnumMember(Value = "run_container_step")]
        RunContainerStep,
    }
    public class HookArgs
    {
    }

    public class PrepareJobArgs : HookArgs
    {
        public HookContainer Container { get; set; }
        public IList<HookContainer> Services { get; set; }
    }

    public class ScriptStepArgs : HookArgs
    {
        public IEnumerable<string> EntryPointArgs { get; set; }
        public string EntryPoint { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public string PrependPath { get; set; }
        public string WorkingDirectory { get; set; }
    }

    public class ContainerRegistry
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ServerUrl { get; set; }
    }

    public class HookContainer : HookArgs
    {
        public string Image { get; set; }
        public string Dockerfile { get; set; }
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
            Image = container.ContainerImage;
            EntryPointArgs = container.ContainerEntryPointArgs?.Split(' ').Select(arg => arg.Trim()) ?? new List<string>();
            EntryPoint = container.ContainerEntryPoint;
            WorkingDirectory = container.ContainerWorkDirectory;
            CreateOptions = container.ContainerCreateOptions;
            if (!string.IsNullOrEmpty(container.RegistryAuthUsername))
            {
                Registry = new ContainerRegistry
                {
                    Username = container.RegistryAuthUsername,
                    Password = container.RegistryAuthPassword,
                    ServerUrl = container.RegistryServer,
                };
            }
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
