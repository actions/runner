using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    public class HookResponse
    {
        public ResponseContext Context { get; set; }
        public JToken State { get; set; }
    }

    public class ResponseContext
    {
        public HookContainer Container { get; set; }
        public IList<HookContainer> Services { get; set; }
    }
}
