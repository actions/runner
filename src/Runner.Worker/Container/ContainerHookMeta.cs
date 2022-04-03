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
        public List<ContainerHookContainer> Containers { get; set; }
    }
    public class ContainerHookContainer : ContainerInfo
    {
    }
}