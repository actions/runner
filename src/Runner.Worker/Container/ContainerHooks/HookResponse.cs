using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookResponse
    {
        public ResponseContext Context { get; set; }
        public JToken State { get; set; }
        public bool? IsAlpine { get; set; }
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
        public IList<string> Ports { get; set; }
    }
}
