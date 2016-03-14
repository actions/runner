using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class BuildCommands : AgentService, ICommandExtension
    {
        public Type ExtensionType 
        {
            get
            {
                return typeof(ICommandExtension);
            }    
        }
        
        public String CommandArea
        {
            get
            {
                return "build";
            }
        }

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            throw new NotImplementedException();
        }
    }
}