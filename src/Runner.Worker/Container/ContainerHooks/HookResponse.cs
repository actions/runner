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
