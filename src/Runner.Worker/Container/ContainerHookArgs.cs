using System;
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
        public ContainerHookContainer Container { get; set; }
    }
    public class ContainerHookContainer
    {
        public string ContainerId { get; set; }
        public string Network { get; set; }
    }
}