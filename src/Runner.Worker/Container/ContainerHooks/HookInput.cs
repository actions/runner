using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookInput
    {
        public HookCommand Command { get; set; }
        public string ResponseFile { get; set; }
        public IHookArgs Args { get; set; }
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
    public interface IHookArgs { }

    public class PrepareJobArgs : IHookArgs
    {
        public HookContainer Container { get; set; }
        public IList<HookContainer> Services { get; set; }
        public string Network { get; set; }
    }

    public class ScriptStepArgs : IHookArgs
    {
        public IEnumerable<string> EntryPointArgs { get; set; }
        public string EntryPoint { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public string PrependPath { get; set; }
        public string WorkingDirectory { get; set; }
        public HookContainer Container { get; internal set; }
    }


    public class ContainerRegistry
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ServerUrl { get; set; }
    }
}