using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Runner.Worker.Container
{
    public class ContainerHookInput
    {
        public ContainerHookCommand Command { get; set; }
        public string ResponseFile { get; set; }
        public ContainerHookArgs Args { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContainerHookCommand
    {
        [EnumMember(Value = "prepare_job")]
        PrepareJob,
        [EnumMember(Value = "cleanup_job")]
        CleanupJob,
    }
    public class ContainerHookArgs
    {
        public HookContainer JobContainer { get; set; }
        public IList<HookContainer> Services { get; set; }
        public string Network { get; set; }
    }

    public class ContainerHookResponse
    {
        public Context Context { get; set; }
    }

    public class Context
    {
        public HookContainer Container { get; set; }
        public IList<HookContainer> Services { get; set; }
    }

    public class State : Dictionary<string, string>
    {
        public ContainerInfo JobContainer { get; set; }
        public ContainerInfo Services { get; set; }
        public Dictionary<string, string> HookCache { get; set; } // For 

    }

    public static class ContainerInfoExtensions
    {
        public static HookContainer GetHookContainer(this ContainerInfo containerInfo)
        {
            return new HookContainer(containerInfo);
        }
    }
}