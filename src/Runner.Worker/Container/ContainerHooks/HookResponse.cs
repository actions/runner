using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookResponse
    {
        public JObject State { get; set; }
        public virtual void Validate(HookInput input) { }
    }
    public class StepHookResponse : HookResponse
    {
        // Keep the JSON token so malformed/string/fractional exit codes cannot be coerced.
        public JToken ExitCode { get; set; }

        public override void Validate(HookInput input)
        {
            if (ExitCode != null &&
                (ExitCode.Type != JTokenType.Integer ||
                 !long.TryParse(ExitCode.ToString(), out var code) || code < 0 || code > 255))
            {
                throw new Exception("The hook response 'exitCode' must be an integer between 0 and 255.");
            }
        }
    }
    public class PrepareJobResponse : HookResponse
    {
        public ResponseContext Context { get; set; }
        public bool? IsAlpine { get; set; }

        public override void Validate(HookInput input)
        {
            bool hasJobContainer = ((PrepareJobArgs)input.Args).Container != null;
            if (hasJobContainer && IsAlpine == null)
            {
                throw new Exception("The property 'isAlpine' is required but was not found in the response file.");
            }
        }
    }
    public class ResponseContext
    {
        public ResponseContainer Container { get; set; }
        public IList<ResponseContainer> Services { get; set; } = new List<ResponseContainer>();
    }
    public class ResponseContainer
    {
        public string Id { get; set; }
        public string Network { get; set; }
        public IDictionary<string, string> Ports { get; set; }
    }
}
