using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookInput
    {
        public HookCommand Command { get; set; }
        public string ResponseFile { get; set; }
        public dynamic Args { get; set; }
        public dynamic State { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum HookCommand
    {
        [EnumMember(Value = "prepare_job")]
        PrepareJob,
        [EnumMember(Value = "cleanup_job")]
        CleanupJob,
    }

    public class PrepareJobArgs
    {
        public HookContainer Container { get; set; }
        public IList<HookContainer> Services { get; set; }
        public string Network { get; set; }
    }

    public class ContainerRegistry
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ServerUrl { get; set; }
    }
}