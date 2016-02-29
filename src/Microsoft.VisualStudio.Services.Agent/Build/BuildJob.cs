using System;

namespace Microsoft.VisualStudio.Services.Agent.Build
{
    public class BuildJob : IJobExtension
    {
        public Type ExtensionType 
        {
            get
            {
                return typeof(IJobExtension);
            }    
        }
        
        public String HostTypes
        {
            get
            {
                return "build";       
            }
        }
    }
}