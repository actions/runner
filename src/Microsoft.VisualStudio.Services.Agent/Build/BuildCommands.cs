using System;

namespace Microsoft.VisualStudio.Services.Agent.Build
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
    }
}