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
        [EnumMember(Value = "run_script_step")]
        RunScriptStep,
    }
    public class HookArgs
    {
        public HookContainer JobContainer { get; set; }
        public IList<HookContainer> Services { get; set; }
        public string Network { get; set; }
    }
    public class HookStepArgs
    {
        public string EntryPointArgs { get; set; }
        public string EntryPoint { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public string PrependPath { get; set; }
        public string WorkingDirectory { get; set; }
        public ContainerInfo Container { get; internal set; }
    }

    public class HookResponse
    {
        public ResponseContext Context { get; set; }
        public dynamic State { get; set; }
    }

    public class ResponseContext
    {
        public HookContainer Container { get; set; }
        public IList<HookContainer> Services { get; set; }
    }

    public static class ContainerInfoExtensions
    {
        public static HookContainer GetHookContainer(this ContainerInfo containerInfo)
        {
            return new HookContainer(containerInfo);
        }
    }
}