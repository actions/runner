using System;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // TODO: Consider eliminating a hard-coded service mapping by resolving the
            // matching class from the same assembly where the interface is defined.
            HostContext context = new HostContext();
            context.RegisterService<IMessageDispatcher, MessageDispatcher>();
            Console.WriteLine("Hello Agent!");
        }
    }
}
