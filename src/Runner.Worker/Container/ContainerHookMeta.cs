using System.Collections.Generic;

namespace GitHub.Runner.Worker.Container
{
    public class ContainerHookMeta
    {
        public string Command { get; set; }
        public string ResponseFile { get; set; }
        public ContainerHookArgs Args { get; set; }
    }
    public class ContainerHookArgs
    {
        public ContainerInfo JobContainer { get; set; }
        public List<ContainerInfo> ServiceContainers { get; set; }
    }

    public class ContainerHookResponse
    {
        public Context Context { get; set; }
    }

    public class Context
    {
        public HookContainer Container { get; set; }
    }

    public class HookContainer
    {
        public string Id { get; set; }
        public string Network { get; set; }
    }

    public class State : Dictionary<string, string>
    {
        public ContainerInfo JobContainer { get; set; }
        public ContainerInfo Services { get; set; }
        public Dictionary<string, string> HookCache { get; set; } // For 
    }
}