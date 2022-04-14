using System.Collections.Generic;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
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
}
