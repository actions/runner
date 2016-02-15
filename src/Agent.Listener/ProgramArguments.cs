using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public sealed class ProgramArguments
    {
        public ProgramArguments(IHostContext context, String[] args)
        {
            var stack = new Stack<String>(args);
            while (stack.Count > 0)
            {
                switch (stack.Peek().ToLowerInvariant())
                {
                    case "--configure":
                        this.Configure = true;
                        break;
                    default:
                        throw new Exception(Resources.GetString("UnknownCommand0", stack.Pop()));
                }

                stack.Pop();
            }
        }

        public Boolean Configure { get; set; }
    }
}
