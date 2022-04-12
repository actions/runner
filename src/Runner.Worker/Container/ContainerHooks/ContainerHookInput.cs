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
        public HookArgs Args { get; set; }
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
    public class HookArgs
    {
        public Container JobContainer { get; set; }
        public IList<Container> Services { get; set; }
        public string Network { get; set; }
    }

    public class HookResponse
    {
        public ResponseContext Context { get; set; }
        public dynamic State { get; set; }
    }

    public class ResponseContext
    {
        public Container Container { get; set; }
        public IList<Container> Services { get; set; }
    }

    public static class ContainerInfoExtensions
    {
        public static Container GetHookContainer(this ContainerInfo containerInfo)
        {
            return new Container(containerInfo);
        }
    }
}